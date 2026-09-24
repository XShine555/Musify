import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';
import { safeReturnTo } from '$lib/server/auth';

export const load: PageServerLoad = ({ locals, url }) => {
	const returnTo = safeReturnTo(url.searchParams.get('returnTo'));
	if (locals.user) {
		redirect(302, returnTo);
	}
	return { returnTo };
};
