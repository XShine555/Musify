import {
	UPLOAD_CONCURRENCY,
	beginUpload,
	chunkStartable,
	failUpload,
	finishCreate,
	finishProcessing,
	finishUpload,
	genreConflictMessage,
	hasActiveUploads,
	isStartable,
	readDuration,
	reportProgress,
	retryUpload,
	summarizeQueue,
	toQueuedUpload,
	type QueuedUpload
} from '$lib/data/uploadQueue';
import type { PresignedAudio } from '$lib/data/uploadProtocol';
import type { GenreOption } from '$lib/types';
import { httpUploadTransport, type UploadTransport } from '$lib/utils/uploadTransport';

const STATUS_BATCH = 20;

interface QueueDeps {
	transport: UploadTransport;
	pollDelay: (attempt: number) => Promise<void>;
	readDuration: (file: File) => Promise<number | null>;
}

interface BatchSettings {
	tags: string[];
	albumId: string | null;
}

function reasonOf(err: unknown): string {
	if (err instanceof DOMException && err.name === 'AbortError') return 'Cancelada';
	return err instanceof Error && err.message !== '' ? err.message : 'La subida falló.';
}

function backoff(attempt: number): Promise<void> {
	const delay = Math.min(3000 * 1.5 ** attempt, 15000);
	return new Promise((resolve) => setTimeout(resolve, delay));
}

export class UploadQueue {
	items = $state<QueuedUpload[]>([]);
	selectedGenres = $state<string[]>([]);
	cover = $state<File | null>(null);
	albumId = $state('');
	notice = $state<string | null>(null);
	running = $state(false);

	summary = $derived(summarizeQueue(this.items));
	active = $derived(hasActiveUploads(this.items));

	private deps: QueueDeps;
	private controllers = new Map<string, AbortController>();
	private presignController: AbortController | null = null;
	private polling = false;

	constructor(deps: Partial<QueueDeps> = {}) {
		this.deps = {
			transport: deps.transport ?? httpUploadTransport,
			pollDelay: deps.pollDelay ?? backoff,
			readDuration: deps.readDuration ?? readDuration
		};
	}

	enqueue(files: File[]) {
		const added = files.map(toQueuedUpload);
		this.items = [...this.items, ...added];
		for (const item of added) {
			if (item.error) continue;
			this.deps.readDuration(item.file).then((duration) => {
				this.update(item.id, (current) => ({ ...current, duration }));
			});
		}
	}

	setTitle(id: string, title: string) {
		this.update(id, (item) => ({ ...item, title }));
	}

	remove(id: string) {
		this.controllers.get(id)?.abort();
		this.items = this.items.filter((item) => item.id !== id);
	}

	cancel(id: string) {
		this.controllers.get(id)?.abort();
	}

	clear() {
		this.presignController?.abort();
		for (const controller of this.controllers.values()) controller.abort();
		this.items = [];
	}

	retry(id: string) {
		this.update(id, retryUpload);
	}

	async start(genreOptions: GenreOption[]) {
		if (this.running) return;
		this.notice = null;
		if (this.selectedGenres.length === 0) {
			this.notice = 'Elige al menos un género para toda la cola.';
			return;
		}
		const conflict = genreConflictMessage(this.selectedGenres, genreOptions);
		if (conflict) {
			this.notice = conflict;
			return;
		}

		const chunks = chunkStartable(this.items);
		if (chunks.length === 0) return;

		const settings: BatchSettings = {
			tags: [...this.selectedGenres],
			albumId: this.albumId === '' ? null : this.albumId
		};
		const cover = this.cover;
		this.running = true;
		try {
			for (const chunk of chunks) await this.runChunk(chunk, cover, settings);
		} finally {
			this.running = false;
		}
	}

	private update(id: string, change: (item: QueuedUpload) => QueuedUpload) {
		this.items = this.items.map((item) => (item.id === id ? change(item) : item));
	}

	private find(id: string): QueuedUpload | undefined {
		return this.items.find((item) => item.id === id);
	}

	private async runChunk(chunk: QueuedUpload[], cover: File | null, settings: BatchSettings) {
		const current = chunk.filter((item) => {
			const live = this.find(item.id);
			return live !== undefined && isStartable(live);
		});
		if (current.length === 0) return;

		const controller = new AbortController();
		this.presignController = controller;

		let audios: PresignedAudio[];
		try {
			const presigned = await this.deps.transport.presign(
				{
					cover: cover ? { fileName: cover.name, contentType: cover.type, size: cover.size } : null,
					files: current.map(({ file }) => ({
						fileName: file.name,
						contentType: file.type,
						size: file.size
					}))
				},
				controller.signal
			);
			if (presigned.picture && cover) {
				await this.deps.transport.put(presigned.picture, cover, () => {}, controller.signal);
			}
			audios = presigned.audios;
		} catch (err) {
			const reason = reasonOf(err);
			for (const { id } of current)
				this.update(id, (item) => failUpload(beginUpload(item), reason));
			return;
		} finally {
			this.presignController = null;
		}

		const jobs = current.map((item, index) => ({ id: item.id, audio: audios[index] }));
		const worker = async () => {
			for (let job = jobs.shift(); job; job = jobs.shift()) {
				await this.processOne(job.id, job.audio, settings);
			}
		};
		await Promise.all(Array.from({ length: Math.min(UPLOAD_CONCURRENCY, jobs.length) }, worker));
	}

	private async processOne(id: string, audio: PresignedAudio, settings: BatchSettings) {
		const item = this.find(id);
		if (!item || !isStartable(item)) return;

		const controller = new AbortController();
		this.controllers.set(id, controller);
		this.update(id, beginUpload);
		try {
			await this.deps.transport.put(
				audio,
				item.file,
				(fraction) => this.update(id, (current) => reportProgress(current, fraction)),
				controller.signal
			);
			this.update(id, finishUpload);
			const title = this.find(id)?.title.trim() || item.title;
			const created = await this.deps.transport.complete(
				{
					title,
					audioIntentId: audio.intentId,
					pictureIntentId: audio.pictureIntentId,
					tags: settings.tags,
					albumId: settings.albumId
				},
				controller.signal
			);
			this.update(id, (current) => finishCreate(current, created.trackId, created.note));
			void this.watchProcessing();
		} catch (err) {
			const reason = reasonOf(err);
			this.update(id, (current) => failUpload(current, reason));
		} finally {
			this.controllers.delete(id);
		}
	}

	private async watchProcessing() {
		if (this.polling) return;
		this.polling = true;
		try {
			for (let attempt = 0; ; attempt++) {
				const trackIds = this.items
					.filter((item) => item.phase === 'processing' && item.trackId !== null)
					.map((item) => item.trackId as string);
				if (trackIds.length === 0) return;
				await this.deps.pollDelay(attempt);
				await this.refreshStatus(trackIds);
			}
		} finally {
			this.polling = false;
		}
	}

	private async refreshStatus(trackIds: string[]) {
		const batches: string[][] = [];
		for (let i = 0; i < trackIds.length; i += STATUS_BATCH) {
			batches.push(trackIds.slice(i, i + STATUS_BATCH));
		}
		await Promise.all(
			batches.map(async (ids) => {
				try {
					const statuses = await this.deps.transport.status(ids);
					for (const item of this.items) {
						const status = item.trackId ? statuses[item.trackId] : undefined;
						if (status === 'Completed') this.update(item.id, finishProcessing);
						if (status === 'Failed') {
							this.update(item.id, (current) =>
								failUpload(current, 'No se pudo procesar el audio.')
							);
						}
					}
				} catch {
					return;
				}
			})
		);
	}
}

export const uploadQueue = new UploadQueue();
