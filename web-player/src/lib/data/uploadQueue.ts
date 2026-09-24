export const UPLOAD_MAX_BYTES = 500 * 1024 * 1024;
export const UPLOAD_FORMATS = ['wav', 'flac', 'aiff', 'aif', 'mp3'];

const UPLOAD_MAX_MB = Math.round(UPLOAD_MAX_BYTES / (1024 * 1024));

export function uploadLimitsLabel(): string {
	const names = UPLOAD_FORMATS.filter((ext) => ext !== 'aif').map((ext) => ext.toUpperCase());
	const list = `${names.slice(0, -1).join(', ')} o ${names[names.length - 1]}`;
	return `${list} · hasta ${UPLOAD_MAX_MB} MB por archivo · puedes soltar carpetas enteras`;
}

export type Visibility = 'private' | 'public';

export interface QueuedUpload {
	id: string;
	file: File;
	title: string;
	format: string;
	error: string | null;
	duration: number | null;
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
		duration: null
	};
}

export function summarizeQueue(items: QueuedUpload[]): { ready: number; failed: number } {
	const failed = items.filter((item) => item.error !== null).length;
	return { ready: items.length - failed, failed };
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
