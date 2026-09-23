import { fail, type RequestEvent } from '@sveltejs/kit';
import { createApiClient, requireAccessTokenAction } from '$lib/server/api';

async function setFollowing({ request, params, locals, fetch }: RequestEvent, follow: boolean) {
	const accessToken = requireAccessTokenAction(locals);
	if (typeof accessToken !== 'string') return accessToken;

	const form = await request.formData();
	const id = String(form.get('userId') ?? '') || String(params.id ?? '');
	if (!id) return fail(400, { message: 'Falta el usuario.' });

	const api = createApiClient({ fetch, accessToken });
	const options = { params: { path: { id } } };
	const { error } = follow
		? await api.POST('/users/{id}/follow', options)
		: await api.DELETE('/users/{id}/follow', options);

	if (error) {
		return fail(502, {
			message: follow ? 'No se pudo seguir al usuario.' : 'No se pudo dejar de seguir al usuario.'
		});
	}

	return { following: follow, userId: id };
}

export const followUserAction = (event: RequestEvent) => setFollowing(event, true);
export const unfollowUserAction = (event: RequestEvent) => setFollowing(event, false);
