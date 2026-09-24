import type { LikedTrack, Track } from '$lib/types';

class LikedStore {
	entries = $state<Record<string, LikedTrack>>({});
	private hydrated = false;

	count = $derived(Object.keys(this.entries).length);
	list = $derived(Object.values(this.entries).sort((a, b) => b.likedAt - a.likedAt));

	hydrate(tracks: LikedTrack[]) {
		if (this.hydrated) return;
		this.hydrated = true;
		this.entries = Object.fromEntries(tracks.map((track) => [track.id, track]));
	}

	isLiked(id: string): boolean {
		return id in this.entries;
	}

	async toggle(track: Track) {
		const id = track.id;
		const before = this.entries;
		const wasLiked = id in before;
		const next = { ...before };
		if (wasLiked) delete next[id];
		else next[id] = { ...track, likedAt: Date.now() };
		this.entries = next;

		try {
			const res = await fetch('/api/likes', {
				method: 'POST',
				headers: { 'content-type': 'application/json' },
				body: JSON.stringify({ trackId: id })
			});
			if (!res.ok) throw new Error('Failed to toggle like');
		} catch {
			const reverted = { ...this.entries };
			if (wasLiked) reverted[id] = before[id];
			else delete reverted[id];
			this.entries = reverted;
		}
	}
}

export const liked = new LikedStore();
