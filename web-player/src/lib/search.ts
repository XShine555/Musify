export const SEARCH_FILTERS = ['Todo', 'Canciones', 'Álbumes', 'Playlists', 'Usuarios'] as const;
export type SearchFilter = (typeof SEARCH_FILTERS)[number];
export const SEARCH_GROUP_PREVIEW = 4;

export function showGroup(filter: SearchFilter, label: Exclude<SearchFilter, 'Todo'>) {
	return filter === 'Todo' || filter === label;
}

export function capped<T>(filter: SearchFilter, list: T[]) {
	return filter === 'Todo' ? list.slice(0, SEARCH_GROUP_PREVIEW) : list;
}

export function matchPlaylists<T extends { name: string }>(playlists: T[], query: string): T[] {
	return query ? playlists.filter((p) => p.name.toLowerCase().includes(query.toLowerCase())) : [];
}

export function searchCounts(counts: {
	tracks: number;
	albums: number;
	playlists: number;
	users: number;
}) {
	return {
		Canciones: counts.tracks,
		Álbumes: counts.albums,
		Playlists: counts.playlists,
		Usuarios: counts.users
	};
}

export function searchChips(counts: ReturnType<typeof searchCounts>) {
	const total = counts.Canciones + counts.Álbumes + counts.Playlists + counts.Usuarios;
	return SEARCH_FILTERS.map((label) => ({
		label,
		count: label === 'Todo' ? total : counts[label]
	}));
}

export type TopResult<Track, Album, Playlist, User> =
	| { kind: 'track'; track: Track }
	| { kind: 'album'; album: Album }
	| { kind: 'playlist'; playlist: Playlist }
	| { kind: 'user'; user: User };

export function findTopResult<
	Track extends { title: string },
	Album extends { title: string },
	Playlist extends { name: string },
	User extends { name: string }
>(params: {
	query: string;
	tracks: Track[];
	albums: Album[];
	playlists: Playlist[];
	users: User[];
}): TopResult<Track, Album, Playlist, User> | null {
	const q = params.query.trim().toLowerCase();
	const startsWithQuery = (value: string) => value.toLowerCase().startsWith(q);

	const userHit = params.users.find((u) => startsWithQuery(u.name));
	if (userHit) return { kind: 'user', user: userHit };
	const trackHit = params.tracks.find((track) => startsWithQuery(track.title));
	if (trackHit) return { kind: 'track', track: trackHit };
	const albumHit = params.albums.find((album) => startsWithQuery(album.title));
	if (albumHit) return { kind: 'album', album: albumHit };
	const playlistHit = params.playlists.find((p) => startsWithQuery(p.name));
	if (playlistHit) return { kind: 'playlist', playlist: playlistHit };

	if (params.users.length) return { kind: 'user', user: params.users[0] };
	if (params.tracks.length) return { kind: 'track', track: params.tracks[0] };
	if (params.albums.length) return { kind: 'album', album: params.albums[0] };
	if (params.playlists.length) return { kind: 'playlist', playlist: params.playlists[0] };
	return null;
}
