import { browser } from '$app/environment';

export interface PlayerTrack {
	id: string;
	title: string;
}

interface StreamResponse {
	manifestUrl: string;
	ticket: string;
	expiresInSeconds: number;
}

class PlayerState {
	queue = $state<PlayerTrack[]>([]);
	index = $state(-1);
	isPlaying = $state(false);
	loading = $state(false);
	currentTime = $state(0);
	duration = $state(0);
	volume = $state(1);
	muted = $state(false);
	error = $state<string | null>(null);

	current = $derived(this.index >= 0 ? (this.queue[this.index] ?? null) : null);
	hasNext = $derived(this.index < this.queue.length - 1);
	hasPrevious = $derived(this.index > 0);

	#audio: HTMLAudioElement | null = null;

	#ensureAudio(): HTMLAudioElement {
		if (this.#audio) return this.#audio;

		const audio = new Audio();
		audio.preload = 'metadata';
		audio.volume = this.volume;

		audio.addEventListener('timeupdate', () => (this.currentTime = audio.currentTime));
		audio.addEventListener(
			'durationchange',
			() => (this.duration = Number.isFinite(audio.duration) ? audio.duration : 0)
		);
		audio.addEventListener('play', () => (this.isPlaying = true));
		audio.addEventListener('pause', () => (this.isPlaying = false));
		audio.addEventListener('waiting', () => (this.loading = true));
		audio.addEventListener('playing', () => (this.loading = false));
		audio.addEventListener('canplay', () => (this.loading = false));
		audio.addEventListener('ended', () => this.next());
		audio.addEventListener('error', () => {
			this.loading = false;
			this.error = 'No se pudo reproducir la pista.';
		});

		this.#audio = audio;
		return audio;
	}

	async playQueue(tracks: PlayerTrack[], startIndex = 0) {
		this.queue = tracks;
		await this.#load(startIndex);
	}

	async #load(index: number) {
		if (!browser || index < 0 || index >= this.queue.length) return;

		this.index = index;
		this.error = null;
		this.loading = true;
		this.currentTime = 0;
		this.duration = 0;

		const audio = this.#ensureAudio();
		try {
			const response = await fetch(`/api/tracks/${this.queue[index].id}/stream`);
			if (!response.ok) throw new Error(String(response.status));

			const stream: StreamResponse = await response.json();
			audio.src = `${stream.manifestUrl}?t=${encodeURIComponent(stream.ticket)}`;
			await audio.play();
		} catch {
			this.loading = false;
			this.error = 'No se pudo reproducir la pista.';
		}
	}

	toggle() {
		const audio = this.#audio;
		if (!audio || !this.current) return;
		if (audio.paused) audio.play();
		else audio.pause();
	}

	next() {
		if (this.hasNext) this.#load(this.index + 1);
		else if (this.#audio) this.#audio.pause();
	}

	previous() {
		if (this.currentTime > 3 || !this.hasPrevious) this.seek(0);
		else this.#load(this.index - 1);
	}

	seek(seconds: number) {
		const audio = this.#audio;
		if (!audio) return;
		audio.currentTime = seconds;
		this.currentTime = seconds;
	}

	setVolume(value: number) {
		this.volume = value;
		this.muted = value === 0;
		if (this.#audio) {
			this.#audio.volume = value;
			this.#audio.muted = this.muted;
		}
	}

	toggleMute() {
		this.muted = !this.muted;
		if (this.#audio) this.#audio.muted = this.muted;
	}
}

export const player = new PlayerState();
