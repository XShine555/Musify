import { toQueueItems, type ApiTrackLike, type TrackSourceKind } from './player.svelte';

export interface LikedTrack {
	id: string;
	title: string;
	artist?: string;
	coverUrl?: string;
	source?: TrackSourceKind;
	explicit?: boolean;
	ownerUserId?: string | number | null;
	likedAt: number;
}

type LikeToggleInput = {
	id: string | number;
	title: string;
	artist?: string;
	coverUrl?: string;
	source?: TrackSourceKind;
	explicit?: boolean;
	ownerUserId?: string | number | null;
};

function toggleRequestBody(track: LikeToggleInput) {
	return track.source === 'youtube'
		? { source: 'YouTube' as const, externalId: String(track.id) }
		: { trackId: String(track.id) };
}

class LikedStore {
	entries = $state<Record<string, LikedTrack>>({});
	private hydrated = false;

	count = $derived(Object.keys(this.entries).length);
	list = $derived(Object.values(this.entries).sort((a, b) => b.likedAt - a.likedAt));

	hydrate(tracks: (ApiTrackLike & { createdAt: string })[]) {
		if (this.hydrated) return;
		this.hydrated = true;

		const items = toQueueItems(tracks);
		const next: Record<string, LikedTrack> = {};
		items.forEach((item, i) => {
			next[String(item.id)] = {
				id: String(item.id),
				title: item.title,
				artist: item.artist,
				coverUrl: item.coverUrl,
				source: item.source,
				explicit: item.explicit,
				ownerUserId: item.ownerUserId,
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
				coverUrl: track.coverUrl,
				source: track.source,
				explicit: track.explicit,
				ownerUserId: track.ownerUserId,
				likedAt: Date.now()
			};
		}
		this.entries = next;

		try {
			const res = await fetch('/api/likes', {
				method: 'POST',
				headers: { 'content-type': 'application/json' },
				body: JSON.stringify(toggleRequestBody(track))
			});
			if (!res.ok) throw new Error('Failed to toggle like');
		} catch {
			this.entries = before;
		}
	}
}

export const liked = new LikedStore();
