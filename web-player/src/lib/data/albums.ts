import type { Paged, Track } from '$lib/types';

async function fetchTracks(url: string): Promise<Track[]> {
	const res = await fetch(url);
	if (!res.ok) return [];
	return ((await res.json()) as Paged<Track>).items;
}

export const fetchAlbumTracks = (albumId: string) => fetchTracks(`/api/albums/${albumId}/tracks`);

export const fetchPlaylistTracks = (playlistId: string) =>
	fetchTracks(`/api/playlists/${playlistId}/tracks`);
