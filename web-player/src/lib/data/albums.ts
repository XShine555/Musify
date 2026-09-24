import { toQueueItems, type ApiTrackLike, type QueueItem } from '$lib/player/player.svelte';

export async function fetchAlbumQueueItems(albumId: string): Promise<QueueItem[]> {
	const res = await fetch(`/api/albums/${albumId}/tracks`);
	if (!res.ok) return [];
	const data = (await res.json()) as { items: ApiTrackLike[] };
	return toQueueItems(data.items ?? []);
}

export async function fetchPlaylistQueueItems(playlistId: string): Promise<QueueItem[]> {
	const res = await fetch(`/api/playlists/${playlistId}/tracks`);
	if (!res.ok) return [];
	const data = (await res.json()) as { items: ApiTrackLike[] };
	return toQueueItems(data.items ?? []);
}
