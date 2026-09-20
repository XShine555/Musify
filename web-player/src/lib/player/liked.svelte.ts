import { toQueueItems, type ApiTrackLike } from './player.svelte';

export interface LikedTrack {
	id: string;
	title: string;
	artist?: string;
	explicit?: boolean;
	ownerUserId?: string | number | null;
	listensCount?: number | string;
	duration?: number | string;
	likedAt: number;
}

type LikeToggleInput = {
	id: string | number;
	title: string;
	artist?: string;
	explicit?: boolean;
	ownerUserId?: string | number | null;
	duration?: number | string;
};

class LikedStore {
	entries = $state<Record<string, LikedTrack>>({});
	private hydrated = false;

	count = $derived(Object.keys(this.entries).length);
	list = $derived(Object.values(this.entries).sort((a, b) => b.likedAt - a.likedAt));

	hydrate(tracks: (ApiTrackLike & { createdAt: string; duration: number | string })[]) {
		if (this.hydrated) return;
		this.hydrated = true;

		const items = toQueueItems(tracks);
		const next: Record<string, LikedTrack> = {};
		items.forEach((item, i) => {
			next[String(item.id)] = {
				id: String(item.id),
				title: item.title,
				artist: item.artist,
				explicit: item.explicit,
				ownerUserId: item.ownerUserId,
				listensCount: item.listensCount,
				duration: tracks[i].duration,
				likedAt: new Date(tracks[i].createdAt).getTime()
			};
		});
		this.entries = next;
	}

	isLiked(id: string | number): boolean {
		return String(id) in this.entries;
	}

	async toggle(track: LikeToggleInput) {
		const id = String(track.id);
		const before = this.entries;
		const wasLiked = id in before;
		const next = { ...before };
		if (wasLiked) delete next[id];
		else {
			next[id] = {
				id,
				title: track.title,
				artist: track.artist,
				explicit: track.explicit,
				ownerUserId: track.ownerUserId,
				duration: track.duration,
				likedAt: Date.now()
			};
		}
		this.entries = next;

		try {
			const res = await fetch('/api/likes', {
				method: 'POST',
				headers: { 'content-type': 'application/json' },
				body: JSON.stringify({ trackId: id })
			});
			if (!res.ok) throw new Error('Failed to toggle like');
		} catch {
			this.entries = before;
		}
	}
}

export const liked = new LikedStore();
