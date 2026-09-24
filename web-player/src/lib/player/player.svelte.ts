import { browser } from '$app/environment';
import { DEFAULT_ACCENT } from '$lib/theme/color';
import { extractAccent } from '$lib/theme/palette';
import { shuffle } from '$lib/data/collections';
import { dialog } from '$lib/state/dialog.svelte';
import { ListenTracker } from './listenTracker';
import { readStorage, writeStorage } from '$lib/utils/storage';
import type { Track } from '$lib/types';
import { append, insertNext, isLastIndex, move, nextIndex, previousIndex } from './queue';

export function isQueueCurrent(items: { id: string }[]): boolean {
	return items.some((item) => item.id === player.currentId);
}

export function playAllOrToggle(items: Track[]) {
	if (items.length === 0) return;
	if (isQueueCurrent(items)) player.toggle();
	else player.playQueue(items, 0);
}

export function playShuffled(items: Track[]) {
	if (items.length === 0) return;
	player.playQueue(shuffle(items), 0);
}

async function readErrorMessage(res: Response): Promise<string> {
	try {
		const body = (await res.json()) as { message?: string };
		return body?.message || `Error ${res.status}`;
	} catch {
		return `Error ${res.status}`;
	}
}

const RECENT_LIMIT = 15;
const DEFAULT_VOLUME = 100;
const VOLUME_KEY = 'musify.player.volume';
const MUTED_KEY = 'musify.player.muted';

function clampVolume(value: number): number {
	return Math.min(100, Math.max(0, Math.round(value)));
}

function loadVolume(): number {
	const stored = readStorage(VOLUME_KEY);
	if (stored === null || stored.trim() === '') return DEFAULT_VOLUME;
	const parsed = Number(stored);
	return Number.isFinite(parsed) ? clampVolume(parsed) : DEFAULT_VOLUME;
}

class PlayerState {
	tracks = $state<Track[]>([]);
	currentId = $state<string | null>(null);
	playing = $state(false);
	progress = $state(0);
	volume = $state(browser ? loadVolume() : DEFAULT_VOLUME);
	muted = $state(browser ? readStorage(MUTED_KEY) === 'true' : false);
	loading = $state(false);
	recentlyPlayed = $state<Track[]>([]);
	shuffle = $state(false);
	repeat = $state(false);

	accentColor = $state<string | null>(null);

	#audio: HTMLAudioElement | null = null;
	#loadToken = 0;
	#accentCache = new Map<string, string>();
	#rafId: number | null = null;
	#listen = new ListenTracker();

	current = $derived(this.tracks.find((t) => t.id === this.currentId) ?? null);
	accent = $derived(this.accentColor ?? DEFAULT_ACCENT);
	duration = $derived(this.current?.duration ?? 0);
	progressPercent = $derived(
		this.duration > 0 ? Math.min(100, (this.progress / this.duration) * 100) : 0
	);

	#audioEl(): HTMLAudioElement | null {
		if (!browser) return null;
		if (this.#audio) return this.#audio;
		const audio = new Audio();
		audio.preload = 'auto';
		audio.volume = this.volume / 100;
		audio.muted = this.muted;
		audio.addEventListener('durationchange', () => this.#syncDuration(audio.duration));
		audio.addEventListener('loadedmetadata', () => this.#syncDuration(audio.duration));
		audio.addEventListener('timeupdate', () => {
			if (!audio.paused) this.#listen.tick(audio.currentTime);
		});
		audio.addEventListener('seeking', () => this.#listen.interrupt());
		audio.addEventListener('seeked', () => this.#listen.resync(audio.currentTime));
		audio.addEventListener('play', () => {
			this.#listen.resync(audio.currentTime);
			this.playing = true;
			this.#startProgressLoop();
			this.#syncMediaSessionPlaybackState();
		});
		audio.addEventListener('pause', () => {
			this.#listen.interrupt();
			this.#listen.flush();
			this.playing = false;
			this.#stopProgressLoop();
			this.#syncMediaSessionPlaybackState();
		});
		audio.addEventListener('ended', () => {
			this.#stopProgressLoop();
			this.#listen.tick(audio.currentTime);
			this.#listen.begin(null);
			if (this.repeat) {
				this.#beginRepeatListen();
				audio.currentTime = 0;
				audio.play().catch(() => {});
				return;
			}
			if (!this.shuffle && isLastIndex(this.tracks.length, this.#index())) {
				this.#stopAtEnd(audio);
				return;
			}
			this.next();
		});
		audio.addEventListener('error', () => {
			this.#stopProgressLoop();
			this.loading = false;
			this.playing = false;
			this.#fail();
		});
		this.#audio = audio;
		document.addEventListener('visibilitychange', () => {
			if (document.visibilityState === 'hidden') this.#listen.flush();
		});
		window.addEventListener('pagehide', () => this.#listen.flush());
		this.#setupMediaSession(audio);
		return audio;
	}

	#stopAtEnd(audio: HTMLAudioElement) {
		audio.currentTime = 0;
		this.progress = 0;
		this.playing = false;
		this.#syncMediaSessionPlaybackState();
	}

	#setupMediaSession(audio: HTMLAudioElement) {
		if (!('mediaSession' in navigator)) return;
		navigator.mediaSession.setActionHandler('play', () => audio.play().catch(() => {}));
		navigator.mediaSession.setActionHandler('pause', () => audio.pause());
		navigator.mediaSession.setActionHandler('previoustrack', () => this.previous());
		navigator.mediaSession.setActionHandler('nexttrack', () => this.next());
	}

	#syncMediaSessionPlaybackState() {
		if (!browser || !('mediaSession' in navigator)) return;
		navigator.mediaSession.playbackState = this.playing ? 'playing' : 'paused';
	}

	#syncMediaSessionMetadata(track: Track) {
		if (!browser || !('mediaSession' in navigator)) return;
		navigator.mediaSession.metadata = new MediaMetadata({
			title: track.title,
			artist: track.artist ?? '',
			artwork: [{ src: `/api/tracks/${track.id}/cover?size=small`, sizes: '256x256' }]
		});
	}

	#clockTime = 0;
	#clockStamp = 0;

	#tickProgress = (now: number) => {
		const audio = this.#audio;
		if (!audio) return;
		if (audio.currentTime !== this.#clockTime) {
			this.#clockTime = audio.currentTime;
			this.#clockStamp = now;
		}
		const elapsed = Math.min(0.25, (now - this.#clockStamp) / 1000);
		const smooth = this.#clockTime + (audio.paused ? 0 : elapsed * audio.playbackRate);
		this.progress = Math.min(smooth, Number.isFinite(audio.duration) ? audio.duration : smooth);
		this.#rafId = requestAnimationFrame(this.#tickProgress);
	};

	#startProgressLoop() {
		if (this.#rafId !== null) return;
		this.#rafId = requestAnimationFrame(this.#tickProgress);
	}

	#stopProgressLoop() {
		if (this.#rafId === null) return;
		cancelAnimationFrame(this.#rafId);
		this.#rafId = null;
	}

	#syncDuration(value: number) {
		if (!Number.isFinite(value) || value <= 0) return;
		const id = this.currentId;
		this.tracks = this.tracks.map((t) => (t.id === id ? { ...t, duration: Math.round(value) } : t));
	}

	#index() {
		return this.tracks.findIndex((t) => t.id === this.currentId);
	}

	#pushRecent(track: Track) {
		this.recentlyPlayed = [track, ...this.recentlyPlayed.filter((t) => t.id !== track.id)].slice(
			0,
			RECENT_LIMIT
		);
	}

	async #applyAccent(id: string) {
		const cached = this.#accentCache.get(id);
		if (cached) {
			this.accentColor = cached;
			return;
		}
		const result = await extractAccent(`/api/tracks/${id}/cover?size=small`);
		if (this.currentId !== id) return;
		if (result) {
			this.#accentCache.set(id, result);
			this.accentColor = result;
		} else {
			this.accentColor = null;
		}
	}

	async #loadCurrent() {
		const audio = this.#audioEl();
		if (!audio || this.currentId === null) return;
		const id = this.currentId;
		const track = this.tracks.find((t) => t.id === id);
		this.#applyAccent(id);
		if (track) this.#syncMediaSessionMetadata(track);
		this.#listen.begin(null);
		const token = ++this.#loadToken;
		this.loading = true;
		this.progress = 0;
		try {
			const { src, listenId } = await this.#resolveLocalSrc(String(id));
			if (token !== this.#loadToken) return;
			audio.src = src;
			audio.volume = this.volume / 100;
			this.#listen.begin(listenId);
			await audio.play();
			this.loading = false;
			if (track) this.#pushRecent(track);
		} catch (err) {
			console.error('playback failed', err);
			if (token !== this.#loadToken) return;
			this.loading = false;
			this.playing = false;
			if (err instanceof DOMException && err.name === 'AbortError') return;
			this.#fail(err instanceof Error && !(err instanceof DOMException) ? err.message : undefined);
		}
	}

	#fail(detail?: string) {
		dialog.error(
			'No se pudo reproducir la pista',
			detail || 'El archivo no está disponible o su formato no es compatible.',
			[{ label: 'Cerrar' }]
		);
	}

	async #resolveLocalSrc(id: string): Promise<{ src: string; listenId: string | null }> {
		const res = await fetch(`/api/tracks/${id}/stream`);
		if (!res.ok) throw new Error(await readErrorMessage(res));
		const { manifestUrl, ticket, listenId } = (await res.json()) as {
			manifestUrl: string;
			ticket: string;
			listenId?: string | null;
		};
		return { src: this.#withTicket(manifestUrl, ticket), listenId: listenId ?? null };
	}

	async #beginRepeatListen() {
		const id = this.currentId;
		if (id === null) return;
		try {
			const res = await fetch(`/api/tracks/${id}/stream`);
			if (!res.ok) return;
			const { listenId } = (await res.json()) as { listenId?: string | null };
			if (this.currentId === id) this.#listen.begin(listenId ?? null);
		} catch {
			return;
		}
	}

	#withTicket(url: string, ticket: string): string {
		if (!ticket) return url;
		const sep = url.includes('?') ? '&' : '?';
		return `${url}${sep}t=${encodeURIComponent(ticket)}`;
	}

	hydrate(track: Track) {
		if (this.currentId !== null || this.tracks.length > 0) return;
		this.tracks = [track];
		this.currentId = track.id;
		this.#applyAccent(track.id);
		this.#syncMediaSessionMetadata(track);
	}

	playQueue(list: Track[], startIndex = 0) {
		if (list.length === 0) return;
		this.tracks = [...list];
		const start = this.tracks[startIndex] ?? this.tracks[0];
		this.currentId = start.id;
		this.#loadCurrent();
	}

	playOrToggle(list: Track[], index: number) {
		if (index < 0 || index >= list.length) return;
		if (this.currentId === list[index].id) this.toggle();
		else this.playQueue(list, index);
	}

	playNext(items: Track[]) {
		if (items.length === 0) return;
		if (this.currentId === null) {
			this.playQueue(items, 0);
			return;
		}
		this.tracks = insertNext(this.tracks, this.#index(), items);
	}

	appendToQueue(items: Track[]) {
		if (items.length === 0) return;
		if (this.currentId === null) {
			this.playQueue(items, 0);
			return;
		}
		this.tracks = append(this.tracks, items);
	}

	toggle() {
		const audio = this.#audioEl();
		if (!audio || this.currentId === null) return;
		if (!audio.src) {
			this.#loadCurrent();
			return;
		}
		if (audio.paused) audio.play().catch(() => {});
		else audio.pause();
	}

	toggleMute() {
		this.#applyMuted(!this.muted);
		if (!this.muted && this.volume === 0) this.setVolume(50);
	}

	#applyMuted(value: boolean) {
		this.muted = value;
		const audio = this.#audioEl();
		if (audio) audio.muted = value;
		if (browser) writeStorage(MUTED_KEY, String(value));
	}

	next() {
		if (this.tracks.length === 0) return;
		const idx = this.#index();
		this.currentId = this.tracks[nextIndex(this.tracks.length, idx, this.shuffle)].id;
		this.#loadCurrent();
	}

	toggleShuffle() {
		this.shuffle = !this.shuffle;
	}

	toggleRepeat() {
		this.repeat = !this.repeat;
	}

	playQueueIndex(index: number) {
		const track = this.tracks[index];
		if (!track) return;
		if (this.currentId === track.id) {
			this.toggle();
			return;
		}
		this.currentId = track.id;
		this.#loadCurrent();
	}

	moveQueueItem(from: number, insertAt: number) {
		this.tracks = move(this.tracks, from, insertAt);
	}

	clearUpcoming() {
		this.tracks = this.current ? [this.current] : [];
	}

	previous() {
		if (this.tracks.length === 0) return;
		const audio = this.#audioEl();
		if (audio && audio.currentTime > 3) {
			audio.currentTime = 0;
			return;
		}
		const idx = this.#index();
		this.currentId = this.tracks[previousIndex(this.tracks.length, idx)].id;
		this.#loadCurrent();
	}

	seekFraction(fraction: number) {
		const audio = this.#audioEl();
		const clamped = Math.min(1, Math.max(0, fraction));
		const target = clamped * this.duration;
		this.progress = target;
		if (audio && Number.isFinite(audio.duration)) audio.currentTime = target;
	}

	setVolume(value: number) {
		this.volume = Number.isFinite(value) ? clampVolume(value) : this.volume;
		if (this.muted && this.volume > 0) this.#applyMuted(false);
		const audio = this.#audioEl();
		if (audio) audio.volume = this.volume / 100;
		if (browser) writeStorage(VOLUME_KEY, String(this.volume));
	}
}

export const player = new PlayerState();
