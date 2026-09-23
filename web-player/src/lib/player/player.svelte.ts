import { browser } from '$app/environment';
import { DEFAULT_ACCENT } from '$lib/theme/color';
import { extractAccent, type Accent } from '$lib/theme/palette';
import { shuffle } from '$lib/data/collections';
import { dialog } from '$lib/state/dialog.svelte';
import { ListenTracker } from './listenTracker';

export interface PlayerTrack {
	id: string | number;
	title: string;
	artist: string;
	duration: number;
	explicit?: boolean;
	ownerUserId?: string | number | null;
	listensCount?: number | string;
}

export interface Playlist {
	id: string | number;
	name: string;
	trackIds: (string | number)[];
}

export interface QueueItem {
	id: string | number;
	title: string;
	artist?: string;
	duration?: number | string;
	explicit?: boolean;
	ownerUserId?: string | number | null;
	listensCount?: number | string;
}

export interface ApiTrackLike {
	id: string | number;
	title: string;
	artist?: string | null;
	duration?: number | string;
	isExplicit?: boolean;
	ownerUserId?: string | number | null;
	listensCount?: number | string;
}

export function toQueueItems(tracks: ApiTrackLike[]): QueueItem[] {
	return tracks.map((track) => ({
		id: track.id,
		title: track.title,
		artist: track.artist ?? undefined,
		duration: track.duration,
		explicit: track.isExplicit,
		ownerUserId: track.ownerUserId,
		listensCount: track.listensCount
	}));
}

export function toQueueItem(track: ApiTrackLike): QueueItem {
	return toQueueItems([track])[0];
}

export function trackFromQueueItem(item: QueueItem): ApiTrackLike {
	return {
		id: item.id,
		title: item.title,
		artist: item.artist,
		duration: item.duration,
		isExplicit: item.explicit,
		ownerUserId: item.ownerUserId,
		listensCount: item.listensCount
	};
}

export function isQueueCurrent(items: { id: string | number }[]): boolean {
	return items.some((item) => item.id === player.current.id);
}

export function playAllOrToggle(items: QueueItem[]) {
	if (items.length === 0) return;
	if (isQueueCurrent(items)) player.toggle();
	else player.playQueue(items, 0);
}

export function playShuffled(items: QueueItem[]) {
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

const EMPTY: PlayerTrack = {
	id: '',
	title: '',
	artist: '',
	duration: 0
};

function toTrack(item: QueueItem): PlayerTrack {
	return {
		id: item.id,
		title: item.title,
		artist: item.artist ?? '',
		duration: Number(item.duration) || 0,
		explicit: item.explicit,
		ownerUserId: item.ownerUserId,
		listensCount: item.listensCount
	};
}

class PlayerState {
	tracks = $state<PlayerTrack[]>([]);
	currentId = $state<string | number | null>(null);
	playing = $state(false);
	progress = $state(0);
	volume = $state(browser ? Number(localStorage.getItem('player.volume') ?? 100) : 100);
	loading = $state(false);
	recentlyPlayed = $state<PlayerTrack[]>([]);
	playlists = $state<Playlist[]>([]);
	shuffle = $state(false);
	repeat = $state(false);

	accentColor = $state<string | null>(null);

	#audio: HTMLAudioElement | null = null;
	#loadToken = 0;
	#accentCache = new Map<string, Accent>();
	#rafId: number | null = null;
	#listen = new ListenTracker();

	current = $derived(this.tracks.find((t) => t.id === this.currentId) ?? EMPTY);
	accent = $derived(this.accentColor ?? DEFAULT_ACCENT);
	progressPercent = $derived(
		this.current.duration > 0 ? Math.min(100, (this.progress / this.current.duration) * 100) : 0
	);

	get isPlaying() {
		return this.playing;
	}

	#audioEl(): HTMLAudioElement | null {
		if (!browser) return null;
		if (this.#audio) return this.#audio;
		const audio = new Audio();
		audio.preload = 'auto';
		audio.volume = this.volume / 100;
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

	#syncMediaSessionMetadata(track: PlayerTrack) {
		if (!browser || !('mediaSession' in navigator)) return;
		if (!track.id) {
			navigator.mediaSession.metadata = null;
			return;
		}
		navigator.mediaSession.metadata = new MediaMetadata({
			title: track.title,
			artist: track.artist,
			artwork: [{ src: `/api/tracks/${track.id}/cover?size=small`, sizes: '256x256' }]
		});
	}

	#tickProgress = () => {
		const audio = this.#audio;
		if (!audio) return;
		this.progress = audio.currentTime;
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

	#pushRecent(track: PlayerTrack) {
		if (!track.id) return;
		this.recentlyPlayed = [track, ...this.recentlyPlayed.filter((t) => t.id !== track.id)].slice(
			0,
			RECENT_LIMIT
		);
	}

	async #applyAccent(id: string | number) {
		const key = String(id);
		const cached = this.#accentCache.get(key);
		if (cached) {
			this.accentColor = cached.accent;
			return;
		}
		const result = await extractAccent(`/api/tracks/${id}/cover?size=small`);
		if (this.currentId !== id) return;
		if (result) {
			this.#accentCache.set(key, result);
			this.accentColor = result.accent;
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
			this.#pushRecent(this.current);
		} catch (exception) {
			console.error('playback failed', exception);
			if (token !== this.#loadToken) return;
			this.loading = false;
			this.playing = false;
			if (exception instanceof DOMException && exception.name === 'AbortError') return;
			this.#fail(
				exception instanceof Error && !(exception instanceof DOMException)
					? exception.message
					: undefined
			);
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

	hydrate(item: ApiTrackLike) {
		if (this.currentId !== null || this.tracks.length > 0) return;
		const track = toTrack(toQueueItem(item));
		this.tracks = [track];
		this.currentId = track.id;
		this.#applyAccent(track.id);
		this.#syncMediaSessionMetadata(track);
	}

	playlistTracks(ids: (string | number)[]): PlayerTrack[] {
		return ids
			.map((id) => this.tracks.find((t) => t.id === id))
			.filter((t): t is PlayerTrack => Boolean(t));
	}

	playQueue(list: QueueItem[], startIndex = 0) {
		if (list.length === 0) return;
		this.tracks = list.map(toTrack);
		const start = this.tracks[startIndex] ?? this.tracks[0];
		this.currentId = start.id;
		this.#loadCurrent();
	}

	playOrToggle(list: QueueItem[], index: number) {
		if (index < 0 || index >= list.length) return;
		if (this.currentId === list[index].id) this.toggle();
		else this.playQueue(list, index);
	}

	playNextItem(item: QueueItem) {
		this.playNext([item]);
	}

	playNext(items: QueueItem[]) {
		if (items.length === 0) return;
		if (this.currentId === null) {
			this.playQueue(items, 0);
			return;
		}
		const tracks = [...this.tracks];
		tracks.splice(this.#index() + 1, 0, ...items.map(toTrack));
		this.tracks = tracks;
	}

	appendToQueue(items: QueueItem[]) {
		if (items.length === 0) return;
		if (this.currentId === null) {
			this.playQueue(items, 0);
			return;
		}
		this.tracks = [...this.tracks, ...items.map(toTrack)];
	}

	playTrack(id: string | number) {
		if (this.currentId === id) {
			this.toggle();
			return;
		}
		const known =
			this.tracks.find((t) => t.id === id) ?? this.recentlyPlayed.find((t) => t.id === id);
		if (known && !this.tracks.some((t) => t.id === id)) {
			this.tracks = [known];
		}
		if (!this.tracks.some((t) => t.id === id)) return;
		this.currentId = id;
		this.#loadCurrent();
	}

	playPlaylist(id: string | number) {
		const playlist = this.playlists.find((p) => p.id === id);
		if (!playlist) return;
		const items = this.playlistTracks(playlist.trackIds);
		if (items.length > 0) this.playQueue(items, 0);
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

	next() {
		if (this.tracks.length === 0) return;
		const idx = this.#index();
		let nextIdx = (idx + 1) % this.tracks.length;
		if (this.shuffle && this.tracks.length > 1) {
			do {
				nextIdx = Math.floor(Math.random() * this.tracks.length);
			} while (nextIdx === idx);
		}
		this.currentId = this.tracks[nextIdx].id;
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

	clearUpcoming() {
		this.tracks = this.currentId === null ? [] : [this.current];
	}

	previous() {
		if (this.tracks.length === 0) return;
		const audio = this.#audioEl();
		if (audio && audio.currentTime > 3) {
			audio.currentTime = 0;
			return;
		}
		const idx = this.#index();
		this.currentId = this.tracks[(idx - 1 + this.tracks.length) % this.tracks.length].id;
		this.#loadCurrent();
	}

	seekFraction(fraction: number) {
		const audio = this.#audioEl();
		const clamped = Math.min(1, Math.max(0, fraction));
		const target = clamped * this.current.duration;
		this.progress = target;
		if (audio && Number.isFinite(audio.duration)) audio.currentTime = target;
	}

	setVolume(value: number) {
		this.volume = Math.min(100, Math.max(0, Math.round(value)));
		const audio = this.#audioEl();
		if (audio) {
			audio.volume = this.volume / 100;
			localStorage.setItem('player.volume', String(this.volume));
		}
	}
}

export const player = new PlayerState();
