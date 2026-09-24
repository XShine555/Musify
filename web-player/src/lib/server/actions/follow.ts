import { fail } from '@sveltejs/kit';
import { authedAction } from '$lib/server/actions/authedAction';

const setFollowing = (follow: boolean) =>
	authedAction(async ({ api, params }) => {
		const id = params.id ?? '';
		if (!id) return fail(400, { message: 'Falta el usuario.' });

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
	});

export const followUserAction = setFollowing(true);
export const unfollowUserAction = setFollowing(false);
