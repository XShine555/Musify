import type { CoauthorInvite, CoauthorStatus } from '$lib/types';
import type { TrackTone } from '$lib/data/libraryTracks';

export interface UserOption {
	id: string;
	name: string;
	profilePictureUrl: string | null;
}

export const COAUTHOR_STATUS: Record<CoauthorStatus, { label: string; tone: TrackTone }> = {
	Pending: { label: 'Pendiente', tone: 'neutral' },
	Accepted: { label: 'Aceptado', tone: 'success' },
	Rejected: { label: 'Rechazado', tone: 'danger' }
};

export function inviteCandidates(
	results: UserOption[],
	{
		ownerId,
		selectedIds,
		invites
	}: { ownerId: string | null; selectedIds: string[]; invites: CoauthorInvite[] }
): UserOption[] {
	const blocked = new Set([
		...selectedIds,
		...invites.filter((invite) => invite.status !== 'Rejected').map((invite) => invite.userId)
	]);
	if (ownerId) blocked.add(ownerId);
	return results.filter((user) => !blocked.has(user.id));
}
