import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError,
	unwrapOrFail
} from '$lib/server/api';
import { ALBUM_TRACKS_PAGE_SIZE, LIBRARY_PICKER_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/albumForm';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [albumRes, tracksRes, libraryRes] = await Promise.all([
		api.GET('/albums/{id}', { params: { path: { id: params.id } } }),
		api.GET('/albums/{albumId}/tracks', {
			params: {
				path: { albumId: params.id },
				query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE }
			}
		}),
		api.GET('/tracks/users/{userId}', {
			params: {
				path: { userId: user.sub },
				query: { pageNumber: 1, pageSize: LIBRARY_PICKER_PAGE_SIZE }
			}
		})
	]);

	const album = unwrapOrError(albumRes, 'Álbum no encontrado.', 404);

	const tracks = tracksRes.data?.items ?? [];
	const inAlbum = new Set(tracks.map((track) => track.id));
	const library = (libraryRes.data?.items ?? []).filter(
		(track) => track.source === 'Local' && !inAlbum.has(track.id)
	);

	const isOwner = Number(album.ownerUserId) === Number(user.sub);

	return { album, tracks, library, isOwner, section: isOwner ? '/albums' : null };
};

export const actions: Actions = {
	addTrack: async ({ request, params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken });
		const result = await api.POST('/albums/{albumId}/tracks/{trackId}', {
			params: { path: { albumId: params.id, trackId } }
		});
		const failure = unwrapOrFail(result, 'No se pudo añadir la canción.');
		if (failure) return failure;
		return { added: true };
	},

	removeTrack: async ({ request, params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken });
		const result = await api.DELETE('/albums/{albumId}/tracks/{trackId}', {
			params: { path: { albumId: params.id, trackId } }
		});
		const failure = unwrapOrFail(result, 'No se pudo quitar la canción.');
		if (failure) return failure;
		return { removed: true };
	},

	edit: async ({ request, params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const parsed = parseAlbumForm(await request.formData());
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

		const api = createApiClient({ fetch, accessToken });
		const result = await api.PUT('/albums/{albumId}', {
			params: { path: { albumId: params.id } },
			body: {
				newTitle: parsed.body.title,
				newDescription: parsed.body.description,
				newReleaseYear: parsed.body.releaseYear
			}
		});
		const failure = unwrapOrFail(result, 'No se pudo actualizar el álbum.');
		if (failure) return failure;
		return { edited: true };
	},

	delete: async ({ params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const api = createApiClient({ fetch, accessToken });
		const result = await api.DELETE('/albums/{albumId}', {
			params: { path: { albumId: params.id } }
		});
		const failure = unwrapOrFail(result, 'No se pudo eliminar el álbum.');
		if (failure) return failure;
		redirect(303, '/albums');
	}
};
