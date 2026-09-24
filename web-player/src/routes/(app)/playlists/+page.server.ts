import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	apiFor,
	authedAction,
	formFile,
	formString,
	requireUser,
	unwrapOrError
} from '$lib/server/api';
import { PLAYLISTS_PAGE_SIZE, PLAYLIST_SUMMARY_TRACKS_PAGE_SIZE } from '$lib/config';
import { toPlaylistSummary, toTrack } from '$lib/server/mappers';
import { uploadPresignedImage } from '$lib/server/upload';

const MAX_NAME = 100;

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const result = await api.GET('/playlists/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: PLAYLISTS_PAGE_SIZE } }
	});

	const data = unwrapOrError(result, 'No se pudieron cargar tus playlists.');

	const tracksByPlaylist = await Promise.all(
		data.items.map((playlist) =>
			api
				.GET('/playlists/{playlistId}/tracks', {
					params: {
						path: { playlistId: playlist.id },
						query: { pageNumber: 1, pageSize: PLAYLIST_SUMMARY_TRACKS_PAGE_SIZE }
					}
				})
				.then((res) => (res.data?.items ?? []).map(toTrack))
		)
	);

	const items = data.items.map((playlist, i) =>
		toPlaylistSummary(playlist, tracksByPlaylist[i].length)
	);

	const totals = {
		playlistCount: items.length,
		trackCount: items.reduce((sum, playlist) => sum + playlist.trackCount, 0),
		durationSeconds: tracksByPlaylist.flat().reduce((sum, track) => sum + track.duration, 0)
	};

	return { playlists: items, totals };
};

export const actions: Actions = {
	create: authedAction(async ({ api, form }) => {
		const name = formString(form, 'name').trim();
		const description = formString(form, 'description').trim();
		const visibility = formString(form, 'visibility') === 'public' ? 'Public' : 'Private';

		if (name === '' || name.length > MAX_NAME) {
			return fail(400, {
				message: 'El nombre es obligatorio (máx. 100 caracteres).',
				name,
				description
			});
		}

		let pictureIntentId: string | null = null;
		const cover = formFile(form, 'cover');
		if (cover) {
			const result = await uploadPresignedImage(
				(args) => api.POST('/playlists/upload-picture', { body: args }),
				cover
			);
			if ('failMessage' in result) {
				return fail(502, { message: result.failMessage, name, description, detail: result.detail });
			}
			pictureIntentId = result.intentId;
		}

		const { data, error } = await api.POST('/playlists', {
			body: { name, description, pictureIntentId, visibility }
		});

		if (error || !data)
			return fail(502, { message: 'No se pudo crear la playlist.', name, description });

		redirect(303, `/playlists/${data.id}`);
	})
};
