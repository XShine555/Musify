import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = ({ locals, url }) => {
	if (locals.user) {
		redirect(302, url.searchParams.get('returnTo') ?? '/');
	}
	return { returnTo: url.searchParams.get('returnTo') ?? '/' };
};
