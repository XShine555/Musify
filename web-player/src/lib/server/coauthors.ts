import { fail } from '@sveltejs/kit';
import { apiErrorDetail, unwrapOrError, type ApiClient } from '$lib/server/api';
import { toCoauthorInvitation, toCoauthorInvite, toCount, toPage } from '$lib/server/mappers';
import {
	COAUTHOR_INVITATIONS_PAGE_SIZE,
	COAUTHOR_INVITES_MAX,
	COAUTHOR_SEARCH_LIMIT
} from '$lib/config';

const INVITE_ERRORS: Record<number, string> = {
	400: 'No puedes invitarte a ti mismo.',
	403: 'Solo el autor de la canción puede invitar coautores.',
	404: 'No se encontró la canción o alguno de los usuarios.',
	409: 'Alguno de los usuarios ya tiene una invitación para esta canción.'
};

const REMOVE_ERRORS: Record<number, string> = {
	403: 'No puedes quitar a este coautor.',
	404: 'La invitación ya no existe.'
};

const RESPOND_ERRORS: Record<number, string> = {
	403: 'Esta invitación no es tuya.',
	404: 'La invitación ya no existe.',
	409: 'Esta invitación ya se respondió.'
};

function failure(
	result: { error?: unknown; response?: { status: number } },
	messages: Record<number, string>,
	fallback: string
) {
	const status = result.response?.status ?? 502;
	return fail(status >= 400 && status < 600 ? status : 502, {
		message: messages[status] ?? fallback,
		detail: apiErrorDetail(result.error)
	});
}

export function parseUserIds(ids: string[]): string[] {
	return [...new Set(ids.map((id) => id.trim()).filter(Boolean))];
}

export async function inviteCoauthors(api: ApiClient, trackId: string, userIds: string[]) {
	const unique = parseUserIds(userIds);
	if (unique.length === 0) return fail(400, { message: 'Elige al menos un usuario.' });
	if (unique.length > COAUTHOR_INVITES_MAX) {
		return fail(400, {
			message: `Puedes invitar a ${COAUTHOR_INVITES_MAX} usuarios como máximo cada vez.`
		});
	}

	const result = await api.POST('/tracks/{id}/coauthors', {
		params: { path: { id: trackId } },
		body: { userIds: unique }
	});
	if (result.error || !result.data) {
		return failure(result, INVITE_ERRORS, 'No se pudieron enviar las invitaciones.');
	}
	return { invites: result.data.map(toCoauthorInvite) };
}

export async function removeCoauthor(api: ApiClient, trackId: string, userId: string) {
	const result = await api.DELETE('/tracks/{id}/coauthors/{userId}', {
		params: { path: { id: trackId, userId } }
	});
	if (result.error) return failure(result, REMOVE_ERRORS, 'No se pudo quitar al coautor.');
	return { removed: userId };
}

export async function respondToInvitation(
	api: ApiClient,
	invitationId: string,
	decision: 'accept' | 'reject'
) {
	const params = { path: { id: invitationId } };
	const result =
		decision === 'accept'
			? await api.POST('/coauthor-invitations/{id}/accept', { params })
			: await api.POST('/coauthor-invitations/{id}/reject', { params });
	if (result.error) {
		return failure(
			result,
			RESPOND_ERRORS,
			decision === 'accept' ? 'No se pudo aceptar la invitación.' : 'No se pudo rechazar.'
		);
	}
	return { invitationId, decision };
}

export async function loadInvitations(api: ApiClient) {
	const result = await api.GET('/users/me/coauthor-invitations', {
		params: { query: { pageNumber: 1, pageSize: COAUTHOR_INVITATIONS_PAGE_SIZE } }
	});
	return toPage(
		unwrapOrError(result, 'No se pudieron cargar las invitaciones.'),
		toCoauthorInvitation
	);
}

export async function loadInvitationCount(api: ApiClient): Promise<number> {
	const result = await api.GET('/users/me/coauthor-invitations', {
		params: { query: { pageNumber: 1, pageSize: 1 } }
	});
	return toCount(result.data?.totalItemCount);
}

export async function searchUsers(api: ApiClient, query: string) {
	const result = await api.GET('/users', {
		params: {
			query: { usernameSearch: query, pageNumber: 1, pageSize: COAUTHOR_SEARCH_LIMIT }
		}
	});
	return (result.data?.items ?? []).map(({ id, name, profilePictureUrl }) => ({
		id,
		name,
		profilePictureUrl
	}));
}
