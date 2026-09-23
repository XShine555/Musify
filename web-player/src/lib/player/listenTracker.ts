const MAX_TICK_GAP_SECONDS = 2.5;
const FLUSH_EVERY_SECONDS = 30;

export class ListenTracker {
	#listenId: string | null = null;
	#played = 0;
	#sent = 0;
	#lastTime: number | null = null;
	#lastFlushAt = 0;

	begin(listenId: string | null | undefined) {
		this.flush();
		this.#listenId = listenId ?? null;
		this.#played = 0;
		this.#sent = 0;
		this.#lastTime = null;
		this.#lastFlushAt = Date.now();
	}

	resync(currentTime: number) {
		this.#lastTime = currentTime;
	}

	interrupt() {
		this.#lastTime = null;
	}

	tick(currentTime: number) {
		if (this.#listenId === null) return;
		const last = this.#lastTime;
		this.#lastTime = currentTime;
		if (last === null) return;
		const delta = currentTime - last;
		if (delta > 0 && delta <= MAX_TICK_GAP_SECONDS) this.#played += delta;
		if (Date.now() - this.#lastFlushAt >= FLUSH_EVERY_SECONDS * 1000) this.flush();
	}

	flush() {
		const id = this.#listenId;
		if (id === null || this.#played <= this.#sent) return;
		this.#sent = this.#played;
		this.#lastFlushAt = Date.now();
		fetch(`/api/listens/${id}/progress`, {
			method: 'PUT',
			headers: { 'content-type': 'application/json' },
			body: JSON.stringify({ playedSeconds: this.#played }),
			keepalive: true
		}).catch(() => {
			this.#sent = 0;
		});
	}
}
