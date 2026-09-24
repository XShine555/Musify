import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { UploadError, parsePresignRequest, presignTrackUploads } from '$lib/server/trackUploads';

export const POST: RequestHandler = async ({ request, locals, fetch }) => {
	if (!locals.accessToken) return json({ message: 'Inicia sesión.' }, { status: 401 });

	try {
		const parsed = parsePresignRequest(await request.json().catch(() => null));
		return json(await presignTrackUploads(apiFor({ fetch, locals }), parsed));
	} catch (err) {
		if (err instanceof UploadError) return json({ message: err.message }, { status: err.status });
		throw err;
	}
};
