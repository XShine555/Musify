export const SEARCH_KINDS = ['tracks', 'albums', 'playlists', 'users'] as const;
export type SearchKind = (typeof SEARCH_KINDS)[number];
export type SearchFilter = 'all' | SearchKind;
export const SEARCH_GROUP_PREVIEW = 4;

export const SEARCH_LABELS: Record<SearchFilter, string> = {
	all: 'Todo',
	tracks: 'Canciones',
	albums: 'Álbumes',
	playlists: 'Playlists',
	users: 'Usuarios'
};

export function showGroup(filter: SearchFilter, kind: SearchKind) {
	return filter === 'all' || filter === kind;
}

export function capped<T>(filter: SearchFilter, list: T[]) {
	return filter === 'all' ? list.slice(0, SEARCH_GROUP_PREVIEW) : list;
}

export function matchPlaylists<T extends { name: string }>(playlists: T[], query: string): T[] {
	return query ? playlists.filter((p) => p.name.toLowerCase().includes(query.toLowerCase())) : [];
}

export type SearchCounts = Record<SearchKind, number>;

export function searchTotal(counts: SearchCounts): number {
	return SEARCH_KINDS.reduce((sum, kind) => sum + counts[kind], 0);
}

export function searchChips(counts: SearchCounts) {
	const filters: SearchFilter[] = ['all', ...SEARCH_KINDS];
	return filters.map((filter) => ({
		filter,
		label: SEARCH_LABELS[filter],
		count: filter === 'all' ? searchTotal(counts) : counts[filter]
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
