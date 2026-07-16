import { browser } from '$app/environment';
import { HUES, accentForHue, gradientForHue } from '$lib/theme/color';
import { extractAccent, type Accent } from '$lib/theme/palette';

export type TrackSourceKind = 'local' | 'youtube';

export interface PlayerTrack {
	id: string | number;
	title: string;
	artist: string;
	duration: number;
	hue: number;
	source: TrackSourceKind;
	coverUrl?: string;
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
	source?: TrackSourceKind;
	coverUrl?: string;
}

export interface ApiTrackLike {
	id: string | number;
	title: string;
	artist?: string;
	source?: 'Local' | 'YouTube';
	externalId?: string;
	audioStatus?: 'Pending' | 'Processing' | 'Completed' | 'Failed';
}

export function isYouTubeTrack(track: ApiTrackLike): boolean {
	return track.source === 'YouTube';
}

export function isPendingYouTubeTrack(track: ApiTrackLike): boolean {
	return isYouTubeTrack(track) && track.audioStatus !== 'Completed';
}

export function youTubeThumbnailUrl(videoId: string): string {
	return `https://i.ytimg.com/vi/${videoId}/mqdefault.jpg`;
}

export function queueIdForTrack(track: ApiTrackLike): string | number {
	return isYouTubeTrack(track) && track.externalId ? track.externalId : track.id;
}

export function toQueueItems(tracks: ApiTrackLike[]): QueueItem[] {
	return tracks.map((track) => {
		const youTube = isYouTubeTrack(track);
		return {
			id: queueIdForTrack(track),
			title: track.title,
			artist: track.artist,
			source: youTube ? 'youtube' : 'local',
			coverUrl: youTube
				? isPendingYouTubeTrack(track) && track.externalId
					? youTubeThumbnailUrl(track.externalId)
					: `/api/tracks/${track.id}/cover?size=small`
				: undefined
		};
	});
}

const RECENT_LIMIT = 15;

const EMPTY: PlayerTrack = { id: '', title: '', artist: '', duration: 0, hue: HUES[0], source: 'local' };

function hueFor(id: string | number): number {
	const text = String(id);
	let hash = 0;
	for (let i = 0; i < text.length; i++) hash = (hash * 31 + text.charCodeAt(i)) >>> 0;
	return HUES[hash % HUES.length];
}

function toTrack(item: QueueItem): PlayerTrack {
	return {
		id: item.id,
		title: item.title,
		artist: item.artist ?? '',
		duration: 0,
		hue: hueFor(item.id),
		source: item.source ?? 'local',
		coverUrl: item.coverUrl
	};
}

class PlayerState {
	tracks = $state<PlayerTrack[]>([]);
	currentId = $state<string | number | null>(null);
	playing = $state(false);
	progress = $state(0);
	volume = $state(
		browser ? Number(localStorage.getItem("player.volume") ?? 100) : 100
	);
	loading = $state(false);
	error = $state('');
	recentlyPlayed = $state<PlayerTrack[]>([]);
	playlists = $state<Playlist[]>([]);

	accentColor = $state<string | null>(null);
	gradientColor = $state<string | null>(null);

	#audio: HTMLAudioElement | null = null;
	#loadToken = 0;
	#retriedId: string | number | null = null;
	#accentCache = new Map<string, Accent>();
	#rafId: number | null = null;

	current = $derived(this.tracks.find((t) => t.id === this.currentId) ?? EMPTY);
	accent = $derived(this.accentColor ?? accentForHue(this.current.hue));
	artGradient = $derived(this.gradientColor ?? gradientForHue(this.current.hue));
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
		audio.addEventListener('play', () => {
			this.playing = true;
			this.#startProgressLoop();
			this.#syncMediaSessionPlaybackState();
		});
		audio.addEventListener('pause', () => {
			this.playing = false;
			this.#stopProgressLoop();
			this.#syncMediaSessionPlaybackState();
		});
		audio.addEventListener('ended', () => {
			this.#stopProgressLoop();
			this.next();
		});
		audio.addEventListener('error', () => {
			this.#stopProgressLoop();
			if (
				this.currentId !== null &&
				this.current.source === 'youtube' &&
				this.#retriedId !== this.currentId
			) {
				this.#retriedId = this.currentId;
				this.#loadCurrent();
				return;
			}
			this.loading = false;
			this.playing = false;
			this.error = 'No se pudo reproducir la pista.';
		});
		this.#audio = audio;
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
		const artwork = track.coverUrl ?? `/api/tracks/${track.id}/cover?size=small`;
		navigator.mediaSession.metadata = new MediaMetadata({
			title: track.title,
			artist: track.artist,
			artwork: [{ src: artwork, sizes: '256x256' }]
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
		this.tracks = this.tracks.map((t) =>
			t.id === id ? { ...t, duration: Math.round(value) } : t
		);
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

	async #applyAccent(id: string | number, coverUrl?: string) {
		const key = String(id);
		const cached = this.#accentCache.get(key);
		if (cached) {
			this.accentColor = cached.accent;
			this.gradientColor = cached.gradient;
			return;
		}
		const result = await extractAccent(coverUrl ?? `/api/tracks/${id}/cover?size=small`);
		if (this.currentId !== id) return;
		if (result) {
			this.#accentCache.set(key, result);
			this.accentColor = result.accent;
			this.gradientColor = result.gradient;
		} else {
			this.accentColor = null;
			this.gradientColor = null;
		}
	}

	async #loadCurrent() {
		const audio = this.#audioEl();
		if (!audio || this.currentId === null) return;
		const id = this.currentId;
		const track = this.tracks.find((t) => t.id === id);
		this.#applyAccent(id, track?.coverUrl);
		if (track) this.#syncMediaSessionMetadata(track);
		const token = ++this.#loadToken;
		this.loading = true;
		this.error = '';
		this.progress = 0;
		try {
			const src =
				track?.source === 'youtube'
					? await this.#resolveYouTubeSrc(String(id))
					: await this.#resolveLocalSrc(String(id));
			if (token !== this.#loadToken) return;
			audio.src = src;
			audio.volume = this.volume / 100;
			await audio.play();
			this.loading = false;
			this.#retriedId = null;
			this.#pushRecent(this.current);
		} catch {
			if (token !== this.#loadToken) return;
			this.loading = false;
			this.playing = false;
			this.error = 'No se pudo reproducir la pista.';
		}
	}

	async #resolveLocalSrc(id: string): Promise<string> {
		const res = await fetch(`/api/tracks/${id}/stream`);
		if (!res.ok) throw new Error(String(res.status));
		const { manifestUrl, ticket } = (await res.json()) as {
			manifestUrl: string;
			ticket: string;
		};
		return this.#withTicket(manifestUrl, ticket);
	}

	async #resolveYouTubeSrc(videoId: string): Promise<string> {
		const res = await fetch(`/api/youtube/tracks/${videoId}/stream`);
		if (!res.ok) throw new Error(String(res.status));
		const { mode, streamUrl, ticket } = (await res.json()) as {
			mode: string;
			streamUrl: string;
			ticket: string;
		};
		return mode === 'Server' ? this.#withTicket(streamUrl, ticket) : streamUrl;
	}

	#withTicket(url: string, ticket: string): string {
		if (!ticket) return url;
		const sep = url.includes('?') ? '&' : '?';
		return `${url}${sep}t=${encodeURIComponent(ticket)}`;
	}

	playlistTracks(ids: (string | number)[]): PlayerTrack[] {
		return ids
			.map((id) => this.tracks.find((t) => t.id === id))
			.filter((t): t is PlayerTrack => Boolean(t));
	}

	mosaicFor(ids: (string | number)[]): string[] {
		const gradients = this.playlistTracks(ids)
			.slice(0, 4)
			.map((t) => gradientForHue(t.hue));
		while (gradients.length < 4) gradients.push('var(--mf-surface)');
		return gradients;
	}

	playQueue(list: QueueItem[], startIndex = 0) {
		if (list.length === 0) return;
		this.tracks = list.map(toTrack);
		const start = this.tracks[startIndex] ?? this.tracks[0];
		this.currentId = start.id;
		this.#loadCurrent();
	}

	addToQueue(item: QueueItem) {
		if (this.currentId === null) {
			this.playQueue([item], 0);
			return;
		}
		this.tracks = [...this.tracks, toTrack(item)];
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
		if (audio.paused) audio.play().catch(() => {});
		else audio.pause();
	}

	next() {
		if (this.tracks.length === 0) return;
		const idx = this.#index();
		this.currentId = this.tracks[(idx + 1) % this.tracks.length].id;
		this.#loadCurrent();
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
			localStorage.setItem("player.volume", String(this.volume));
		}
	}
}

export const player = new PlayerState();
