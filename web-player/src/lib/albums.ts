import { toQueueItems, type ApiTrackLike, type QueueItem } from '$lib/player/player.svelte';

interface YouTubeAlbumTracksResponse {
	album: { artist: string; thumbnailUrl: string };
	tracks: { videoId: string; title: string; isExplicit: boolean }[];
}

export async function fetchAlbumQueueItems(
	kind: 'local' | 'youtube',
	albumId: string
): Promise<QueueItem[]> {
	if (kind === 'local') {
		const res = await fetch(`/api/albums/${albumId}/tracks`);
		if (!res.ok) return [];
		const data = (await res.json()) as { items: ApiTrackLike[] };
		return toQueueItems(data.items ?? []);
	}

	const res = await fetch(`/api/albums/youtube/${albumId}/tracks`);
	if (!res.ok) return [];
	const detail = (await res.json()) as YouTubeAlbumTracksResponse;
	return detail.tracks.map((track) => ({
		id: track.videoId,
		title: track.title,
		artist: detail.album.artist,
		source: 'youtube' as const,
		coverUrl: detail.album.thumbnailUrl,
		explicit: track.isExplicit
	}));
}
