import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError
} from '$lib/server/api';
import { uploadPresignedImage } from '$lib/server/upload';

const MAX_NAME = 100;

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await api.GET('/playlists/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 50 } }
	});

	const data = unwrapOrError(result, 'No se pudieron cargar tus playlists.');

	const tracksByPlaylist = await Promise.all(
		data.items.map((playlist) =>
			api
				.GET('/playlists/{playlistId}/tracks', {
					params: { path: { playlistId: playlist.id }, query: { pageNumber: 1, pageSize: 500 } }
				})
				.then((res) => res.data?.items ?? [])
		)
	);

	const items = data.items.map((playlist, i) => {
		const tracks = tracksByPlaylist[i];
		const trackCount = tracks.length;
		const durationSeconds = tracks.reduce((sum, t) => sum + Number(t.duration || 0), 0);
		return { ...playlist, trackCount, durationSeconds };
	});

	const totals = {
		playlistCount: items.length,
		trackCount: items.reduce((sum, p) => sum + p.trackCount, 0),
		durationSeconds: items.reduce((sum, p) => sum + p.durationSeconds, 0)
	};

	return { playlists: { ...data, items }, totals };
};

export const actions: Actions = {
	create: async ({ request, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const form = await request.formData();
		const name = String(form.get('name') ?? '').trim();
		const description = String(form.get('description') ?? '').trim();
		const visibility = form.get('visibility') === 'public' ? 'Public' : 'Private';
		const cover = form.get('cover');

		if (name === '' || name.length > MAX_NAME) {
			return fail(400, {
				message: 'El nombre es obligatorio (máx. 100 caracteres).',
				name,
				description
			});
		}

		const api = createApiClient({ fetch, accessToken });

		let pictureIntentId: string | null = null;
		if (cover instanceof File && cover.size > 0) {
			const result = await uploadPresignedImage(
				(args) => api.POST('/playlists/upload-picture', { body: args }),
				cover
			);
			if ('failMessage' in result) {
				return fail(502, { message: result.failMessage, name, description, detail: result.detail });
			}
			pictureIntentId = result.intentId;
		}

		const { data, error: err } = await api.POST('/playlists', {
			body: { name, description, pictureIntentId, visibility }
		});

		if (err || !data)
			return fail(502, { message: 'No se pudo crear la playlist.', name, description });

		redirect(303, `/playlists/${data.id}`);
	}
};
