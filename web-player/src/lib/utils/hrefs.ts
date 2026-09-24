type CoverSize = 'small' | 'medium' | 'large';

export function searchHref(query: string): string {
	return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
}

export function genreHref(genre: string): string {
	return `/explore?genre=${encodeURIComponent(genre)}`;
}

export function trackCover(id: string, size: CoverSize): string {
	return `/api/tracks/${id}/cover?size=${size}`;
}

export function albumCover(id: string, size: CoverSize): string {
	return `/api/albums/${id}/cover?size=${size}`;
}

export function playlistCover(
	playlist: { id: string; updatedAt: string },
	size: CoverSize
): string {
	return `/api/playlists/${playlist.id}/cover?size=${size}&v=${encodeURIComponent(playlist.updatedAt)}`;
}
