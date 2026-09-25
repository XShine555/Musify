import type { PageServerLoad, Actions } from './$types';
import { fail } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { formString } from '$lib/server/forms/fields';
import { tagged } from '$lib/server/actions/tagged';
import { loadInvitations, respondToInvitation } from '$lib/server/coauthors';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	requireUser(locals, url);
	return { invitations: await loadInvitations(apiFor({ fetch, locals })) };
};

function respond(decision: 'accept' | 'reject') {
	return tagged(decision, async ({ api, form }) => {
		const invitationId = formString(form, 'invitationId');
		if (!invitationId) return fail(400, { message: 'Falta la invitación.' });
		return respondToInvitation(api, invitationId, decision);
	});
}

export const actions: Actions = {
	accept: respond('accept'),
	reject: respond('reject')
};
