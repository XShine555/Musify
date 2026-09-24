import { findConflict, genreInfo } from '$lib/data/genres';
import type { GenreOption } from '$lib/types';

export const UPLOAD_MAX_BYTES = 500 * 1024 * 1024;
export const UPLOAD_FORMATS = ['wav', 'flac', 'aiff', 'aif', 'mp3'];

const UPLOAD_MAX_MB = Math.round(UPLOAD_MAX_BYTES / (1024 * 1024));

export function uploadLimitsLabel(): string {
	const names = UPLOAD_FORMATS.filter((ext) => ext !== 'aif').map((ext) => ext.toUpperCase());
	const list = `${names.slice(0, -1).join(', ')} o ${names[names.length - 1]}`;
	return `${list} · hasta ${UPLOAD_MAX_MB} MB por archivo · puedes soltar carpetas enteras`;
}

export const UPLOAD_CONCURRENCY = 3;
export const UPLOAD_BATCH_SIZE = 20;

export type UploadPhase = 'queued' | 'uploading' | 'creating' | 'processing' | 'done' | 'error';

export interface QueuedUpload {
	id: string;
	file: File;
	title: string;
	format: string;
	error: string | null;
	duration: number | null;
	phase: UploadPhase;
	progress: number;
	failure: string | null;
	trackId: string | null;
	note: string | null;
}

export function fileExtension(name: string): string {
	const dot = name.lastIndexOf('.');
	return dot < 0 ? '' : name.slice(dot + 1).toLowerCase();
}

export function titleFromFileName(name: string): string {
	const base = name.replace(/\.[^.]+$/, '');
	const cleaned = base
		.replace(/^\d{1,3}\s*[-_.)]\s*/, '')
		.replace(/[_]+/g, ' ')
		.replace(/\s+/g, ' ')
		.trim();
	if (cleaned === '') return base;
	return cleaned.charAt(0).toUpperCase() + cleaned.slice(1);
}

export function uploadError(file: File): string | null {
	if (!UPLOAD_FORMATS.includes(fileExtension(file.name))) return 'Formato no compatible';
	if (file.size > UPLOAD_MAX_BYTES) return `Supera ${UPLOAD_MAX_MB} MB`;
	if (file.size === 0) return 'Archivo vacío';
	return null;
}

export function toQueuedUpload(file: File): QueuedUpload {
	return {
		id: crypto.randomUUID(),
		file,
		title: titleFromFileName(file.name),
		format: fileExtension(file.name).toUpperCase() || '?',
		error: uploadError(file),
		duration: null,
		phase: 'queued',
		progress: 0,
		failure: null,
		trackId: null,
		note: null
	};
}

export function isStartable(item: QueuedUpload): boolean {
	return item.error === null && item.phase === 'queued';
}

export function isBusy(item: QueuedUpload): boolean {
	return item.phase === 'uploading' || item.phase === 'creating';
}

export function isActive(item: QueuedUpload): boolean {
	return isBusy(item) || item.phase === 'processing';
}

export function beginUpload(item: QueuedUpload): QueuedUpload {
	if (!isStartable(item)) return item;
	return { ...item, phase: 'uploading', progress: 0, failure: null };
}

export function reportProgress(item: QueuedUpload, fraction: number): QueuedUpload {
	if (item.phase !== 'uploading') return item;
	const progress = Math.min(1, Math.max(item.progress, fraction));
	return progress === item.progress ? item : { ...item, progress };
}

export function finishUpload(item: QueuedUpload): QueuedUpload {
	if (item.phase !== 'uploading') return item;
	return { ...item, phase: 'creating', progress: 1 };
}

export function finishCreate(
	item: QueuedUpload,
	trackId: string,
	note: string | null = null
): QueuedUpload {
	if (item.phase !== 'creating') return item;
	return { ...item, phase: 'processing', trackId, note };
}

export function finishProcessing(item: QueuedUpload): QueuedUpload {
	if (item.phase !== 'processing') return item;
	return { ...item, phase: 'done' };
}

export function failUpload(item: QueuedUpload, reason: string): QueuedUpload {
	if (!isActive(item)) return item;
	return { ...item, phase: 'error', failure: reason };
}

export function retryUpload(item: QueuedUpload): QueuedUpload {
	if (item.phase !== 'error') return item;
	return { ...item, phase: 'queued', progress: 0, failure: null, trackId: null, note: null };
}

export function genreConflictMessage(tags: string[], options: GenreOption[]): string | undefined {
	const conflict = findConflict(tags, options);
	if (!conflict) return undefined;
	return `Los géneros ${genreInfo(conflict[0]).label} y ${genreInfo(conflict[1]).label} no se pueden combinar.`;
}

export function chunkStartable(items: QueuedUpload[], size = UPLOAD_BATCH_SIZE): QueuedUpload[][] {
	const startable = items.filter(isStartable);
	const chunks: QueuedUpload[][] = [];
	for (let i = 0; i < startable.length; i += size) chunks.push(startable.slice(i, i + size));
	return chunks;
}

export function hasActiveUploads(items: QueuedUpload[]): boolean {
	return items.some(isActive);
}

export function summarizeQueue(items: QueuedUpload[]): {
	ready: number;
	failed: number;
	uploading: number;
	processing: number;
	done: number;
	errored: number;
} {
	const invalid = items.filter((item) => item.error !== null).length;
	const valid = items.filter((item) => item.error === null);
	return {
		ready: valid.filter((item) => item.phase === 'queued').length,
		failed: invalid,
		uploading: valid.filter(isBusy).length,
		processing: valid.filter((item) => item.phase === 'processing').length,
		done: valid.filter((item) => item.phase === 'done').length,
		errored: valid.filter((item) => item.phase === 'error').length
	};
}

async function readEntries(reader: FileSystemDirectoryReader): Promise<FileSystemEntry[]> {
	const all: FileSystemEntry[] = [];
	for (;;) {
		const batch = await new Promise<FileSystemEntry[]>((resolve, reject) =>
			reader.readEntries(resolve, reject)
		);
		if (batch.length === 0) return all;
		all.push(...batch);
	}
}

async function filesFromEntry(entry: FileSystemEntry): Promise<File[]> {
	if (entry.isFile) {
		const file = await new Promise<File>((resolve, reject) =>
			(entry as FileSystemFileEntry).file(resolve, reject)
		);
		return [file];
	}
	if (!entry.isDirectory) return [];
	const children = await readEntries((entry as FileSystemDirectoryEntry).createReader());
	return (await Promise.all(children.map(filesFromEntry))).flat();
}

export async function filesFromDrop(transfer: DataTransfer): Promise<File[]> {
	const entries = Array.from(transfer.items)
		.map((item) => item.webkitGetAsEntry?.() ?? null)
		.filter((entry): entry is FileSystemEntry => entry !== null);
	if (entries.length === 0) return Array.from(transfer.files);
	return (await Promise.all(entries.map(filesFromEntry))).flat();
}

export function readDuration(file: File): Promise<number | null> {
	return new Promise((resolve) => {
		const url = URL.createObjectURL(file);
		const audio = new Audio();
		const done = (value: number | null) => {
			URL.revokeObjectURL(url);
			resolve(value);
		};
		audio.preload = 'metadata';
		audio.onloadedmetadata = () => done(Number.isFinite(audio.duration) ? audio.duration : null);
		audio.onerror = () => done(null);
		audio.src = url;
	});
}
