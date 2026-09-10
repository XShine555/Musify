import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';
import { getAllowAnonymousListening } from '$lib/server/playbackConfig';

const PUBLIC_PATHS = new Set(['/login']);

export const load: LayoutServerLoad = async ({ locals, url, fetch }) => {
	const allowAnonymousListening = await getAllowAnonymousListening(fetch);

	if (!locals.user && !allowAnonymousListening && !PUBLIC_PATHS.has(url.pathname)) {
		redirect(302, `/login?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
	}
	return { user: locals.user, allowAnonymousListening };
};
