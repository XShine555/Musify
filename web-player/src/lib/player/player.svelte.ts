import { browser } from '$app/environment';
import { dialog } from '$lib/state/dialog.svelte';
import type { Track } from '$lib/types';
import { readStorage, writeStorage } from '$lib/utils/storage';
import { ListenTracker } from './listenTracker';
import { mediaSession } from './mediaSession';
import { ProgressClock } from './progressClock';
import { append, insertNext, isLastIndex, move, nextIndex, previousIndex } from './queue';
import { resolveStream } from './stream';

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

	#audio: HTMLAudioElement | null = null;
	#loadToken = 0;
	#listen = new ListenTracker();
	#clock = new ProgressClock(
		() => this.#audio,
		(value) => (this.progress = value)
	);

	current = $derived(this.tracks.find((t) => t.id === this.currentId) ?? null);
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
			this.#clock.start();
			mediaSession.setPlaying(true);
		});
		audio.addEventListener('pause', () => {
			this.#listen.interrupt();
			this.#listen.flush();
			this.playing = false;
			this.#clock.stop();
			mediaSession.setPlaying(false);
		});
		audio.addEventListener('ended', () => {
			this.#clock.stop();
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
			this.#clock.stop();
			this.loading = false;
			this.playing = false;
			this.#fail();
		});
		this.#audio = audio;
		document.addEventListener('visibilitychange', () => {
			if (document.visibilityState === 'hidden') this.#listen.flush();
		});
		window.addEventListener('pagehide', () => this.#listen.flush());
		mediaSession.setup({
			play: () => audio.play().catch(() => {}),
			pause: () => audio.pause(),
			previous: () => this.previous(),
			next: () => this.next()
		});
		return audio;
	}

	#stopAtEnd(audio: HTMLAudioElement) {
		audio.currentTime = 0;
		this.progress = 0;
		this.playing = false;
		mediaSession.setPlaying(false);
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

	#announce(track: Track) {
		mediaSession.setTrack(track);
	}

	async #loadCurrent() {
		const audio = this.#audioEl();
		if (!audio || this.currentId === null) return;
		const id = this.currentId;
		const track = this.tracks.find((t) => t.id === id);
		if (track) this.#announce(track);
		this.#listen.begin(null);
		const token = ++this.#loadToken;
		this.loading = true;
		this.progress = 0;
		try {
			const { src, listenId } = await resolveStream(id);
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

	async #beginRepeatListen() {
		const id = this.currentId;
		if (id === null) return;
		try {
			const { listenId } = await resolveStream(id);
			if (this.currentId === id) this.#listen.begin(listenId);
		} catch {
			return;
		}
	}

	hydrate(track: Track) {
		if (this.currentId !== null || this.tracks.length > 0) return;
		this.tracks = [track];
		this.currentId = track.id;
		this.#announce(track);
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
