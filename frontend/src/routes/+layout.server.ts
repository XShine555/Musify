import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

const PUBLIC_PATHS = new Set(['/login']);

export const load: LayoutServerLoad = ({ locals, url }) => {
	if (!locals.user && !PUBLIC_PATHS.has(url.pathname)) {
		redirect(302, `/login?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
	}
	return { user: locals.user };
};
