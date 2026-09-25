import { describe, expect, it } from 'vitest';
import { inviteCandidates } from '$lib/data/coauthors';

const user = (id: string) => ({ id, name: `User ${id}`, profilePictureUrl: null });

describe('inviteCandidates', () => {
	it('hides the owner, selected users and users with an active invitation', () => {
		const result = inviteCandidates([user('owner'), user('a'), user('b'), user('c'), user('d')], {
			ownerId: 'owner',
			selectedIds: ['a'],
			invites: [
				{ userId: 'b', name: 'B', status: 'Pending' },
				{ userId: 'c', name: 'C', status: 'Accepted' }
			]
		});
		expect(result.map((item) => item.id)).toEqual(['d']);
	});

	it('keeps users who rejected so they can be invited again', () => {
		const result = inviteCandidates([user('a')], {
			ownerId: null,
			selectedIds: [],
			invites: [{ userId: 'a', name: 'A', status: 'Rejected' }]
		});
		expect(result.map((item) => item.id)).toEqual(['a']);
	});
});
