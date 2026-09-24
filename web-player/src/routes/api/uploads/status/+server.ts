import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { UploadError, parseStatusIds, trackProcessingStatus } from '$lib/server/trackUploads';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	if (!locals.accessToken) return json({ message: 'Inicia sesión.' }, { status: 401 });

	try {
		const ids = parseStatusIds(url.searchParams.get('ids'));
		return json(await trackProcessingStatus(apiFor({ fetch, locals }), ids));
	} catch (err) {
		if (err instanceof UploadError) return json({ message: err.message }, { status: err.status });
		throw err;
	}
};
