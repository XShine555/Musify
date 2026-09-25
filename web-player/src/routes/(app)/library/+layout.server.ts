import type { LayoutServerLoad } from './$types';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { loadInvitationCount } from '$lib/server/coauthors';

export const load: LayoutServerLoad = async ({ locals, url, fetch }) => {
	requireUser(locals, url);
	return { invitationCount: await loadInvitationCount(apiFor({ fetch, locals })) };
};
