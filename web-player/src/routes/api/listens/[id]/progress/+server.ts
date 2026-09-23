import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

export const PUT: RequestHandler = async ({ params, request, locals, fetch }) => {
	if (!locals.accessToken) return json({ message: 'Inicia sesión.' }, { status: 401 });

	const { playedSeconds } = (await request.json()) as { playedSeconds?: number };
	if (typeof playedSeconds !== 'number' || !Number.isFinite(playedSeconds) || playedSeconds < 0)
		return json({ message: 'Tiempo inválido.' }, { status: 400 });

	const api = createApiClient({ fetch, accessToken: locals.accessToken });
	const result = await api.PUT('/tracks/listens/{listenId}/progress', {
		params: { path: { listenId: params.id } },
		body: { playedSeconds }
	});

	if (result.error !== undefined)
		return json(
			{ message: 'No se pudo registrar la escucha.' },
			{ status: result.response.status === 404 ? 404 : 502 }
		);

	return new Response(null, { status: 204 });
};
