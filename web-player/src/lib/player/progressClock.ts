const MAX_INTERPOLATION_SECONDS = 0.25;

export class ProgressClock {
	#rafId: number | null = null;
	#time = 0;
	#stamp = 0;

	constructor(
		private getAudio: () => HTMLAudioElement | null,
		private onProgress: (seconds: number) => void
	) {}

	#tick = (now: number) => {
		const audio = this.getAudio();
		if (!audio) return;
		if (audio.currentTime !== this.#time) {
			this.#time = audio.currentTime;
			this.#stamp = now;
		}
		const elapsed = Math.min(MAX_INTERPOLATION_SECONDS, (now - this.#stamp) / 1000);
		const smooth = this.#time + (audio.paused ? 0 : elapsed * audio.playbackRate);
		this.onProgress(Math.min(smooth, Number.isFinite(audio.duration) ? audio.duration : smooth));
		this.#rafId = requestAnimationFrame(this.#tick);
	};

	start() {
		if (this.#rafId !== null) return;
		this.#rafId = requestAnimationFrame(this.#tick);
	}

	stop() {
		if (this.#rafId === null) return;
		cancelAnimationFrame(this.#rafId);
		this.#rafId = null;
	}
}
