import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';

export const POST: RequestHandler = async ({ request, locals, fetch }) => {
	if (!locals.accessToken) return json({ message: 'Inicia sesión.' }, { status: 401 });

	const payload = await request.json().catch(() => null);
	const trackId = payload?.trackId;
	if (typeof trackId !== 'string' || trackId === '')
		return json({ message: 'Canción inválida.' }, { status: 400 });

	const api = apiFor({ fetch, locals });
	const result = await api.POST('/likes/toggle', { body: { trackId } });

	if (result.error || result.data === undefined)
		return json({ message: 'No se pudo actualizar Me gusta.' }, { status: 502 });

	return json({ liked: result.data });
};
