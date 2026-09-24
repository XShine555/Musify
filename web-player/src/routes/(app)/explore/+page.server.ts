import type { PageServerLoad, Actions } from './$types';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { toAlbum, toPage, toTrack } from '$lib/server/mappers';
import { addAlbumToPlaylistAction, addTrackAction } from '$lib/server/playlistActions';
import { EXPLORE_ALBUMS_PAGE_SIZE, EXPLORE_PAGE_SIZE, EXPLORE_USERS_LIMIT } from '$lib/config';

export const load: PageServerLoad = async ({ url, locals, fetch }) => {
	const query = url.searchParams.get('q')?.trim() ?? '';
	const genreParam = url.searchParams.get('genre')?.trim() || null;
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

	const user = locals.user;
	const api = apiFor({ fetch, locals });

	const genresRes = await api.GET('/genres');
	const genres = genresRes.data ?? [];
	const genre = genres.find((item) => item.genre === genreParam)?.genre ?? null;

	const tracksPromise = api.GET('/tracks', {
		params: {
			query: {
				name: query || undefined,
				genre: genre ?? undefined,
				pageNumber: page,
				pageSize: EXPLORE_PAGE_SIZE
			}
		}
	});

	const albumsPromise = query
		? api.GET('/albums', {
				params: { query: { title: query, pageNumber: 1, pageSize: EXPLORE_ALBUMS_PAGE_SIZE } }
			})
		: Promise.resolve(null);

	const usersPromise = query
		? api.GET('/users', {
				params: { query: { usernameSearch: query, pageNumber: 1, pageSize: EXPLORE_USERS_LIMIT } }
			})
		: Promise.resolve(null);

	const [tracksRes, albumsRes, usersRes] = await Promise.all([
		tracksPromise,
		albumsPromise,
		usersPromise
	]);

	const tracks = toPage(unwrapOrError(tracksRes, 'No se pudieron cargar las canciones.'), (item) =>
		toTrack(item.track)
	);

	return {
		query,
		genre,
		genres,
		tracks,
		albums: (albumsRes?.data?.items ?? []).map((item) => toAlbum(item.album)),
		albumsTotal: Number(albumsRes?.data?.totalItemCount ?? 0),
		users: usersRes?.data?.items ?? [],
		usersTotal: Number(usersRes?.data?.totalItemCount ?? 0),
		viewerId: user?.sub ?? null
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addAlbumToPlaylist: addAlbumToPlaylistAction
};
