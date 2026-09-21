import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

export const POST: RequestHandler = async ({ request, locals, fetch }) => {
	if (!locals.accessToken) return json({ message: 'Inicia sesión.' }, { status: 401 });

	const body = await request.json();
	const api = createApiClient({ fetch, accessToken: locals.accessToken });
	const result = await api.POST('/likes/toggle', { body });

	if (result.error || result.data === undefined)
		return json({ message: 'No se pudo actualizar Me gusta.' }, { status: 502 });

	return json({ liked: result.data });
};
