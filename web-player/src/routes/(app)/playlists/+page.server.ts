import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import { apiFor, authedAction, requireUser, unwrapOrError } from '$lib/server/api';
import { PLAYLISTS_PAGE_SIZE } from '$lib/config';
import { toPlaylist } from '$lib/server/mappers';
import { parsePlaylistForm } from '$lib/server/forms/playlistForm';
import { uploadOptionalCover } from '$lib/server/upload';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const result = await api.GET('/playlists/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: PLAYLISTS_PAGE_SIZE } }
	});

	const playlists = unwrapOrError(result, 'No se pudieron cargar tus playlists.').items.map(
		toPlaylist
	);

	const totals = {
		playlistCount: playlists.length,
		trackCount: playlists.reduce((sum, playlist) => sum + playlist.trackCount, 0),
		durationSeconds: playlists.reduce((sum, playlist) => sum + playlist.durationSeconds, 0)
	};

	return { playlists, totals };
};

export const actions: Actions = {
	create: authedAction(async ({ api, form }) => {
		const parsed = parsePlaylistForm(form);
		if ('failMessage' in parsed) {
			const { failMessage: message, name, description } = parsed;
			return fail(400, { message, name, description });
		}
		const { name, description, visibility, cover } = parsed.body;

		const uploaded = await uploadOptionalCover(api, '/playlists/upload-picture', cover);
		if ('failMessage' in uploaded) {
			return fail(502, {
				message: uploaded.failMessage,
				name,
				description,
				detail: uploaded.detail
			});
		}

		const { data, error } = await api.POST('/playlists', {
			body: { name, description, pictureIntentId: uploaded.intentId, visibility }
		});

		if (error || !data)
			return fail(502, { message: 'No se pudo crear la playlist.', name, description });

		redirect(303, `/playlists/${data.id}`);
	})
};
