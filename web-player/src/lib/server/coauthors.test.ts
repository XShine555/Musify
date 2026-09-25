import { isActionFailure } from '@sveltejs/kit';
import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from '$lib/server/api';
import {
	inviteCoauthors,
	loadInvitationCount,
	parseUserIds,
	removeCoauthor,
	respondToInvitation,
	searchUsers
} from '$lib/server/coauthors';

const asApi = (fake: unknown) => fake as ApiClient;

function failed(status: number) {
	return { error: { status }, response: { status } };
}

describe('parseUserIds', () => {
	it('trims, drops blanks and removes duplicates', () => {
		expect(parseUserIds([' a ', '', 'b', 'a'])).toEqual(['a', 'b']);
	});
});

describe('inviteCoauthors', () => {
	it('rejects an empty selection without calling the API', async () => {
		const POST = vi.fn();
		const result = await inviteCoauthors(asApi({ POST }), 't1', ['', ' ']);
		expect(isActionFailure(result)).toBe(true);
		expect(POST).not.toHaveBeenCalled();
	});

	it('rejects more users than the API accepts', async () => {
		const POST = vi.fn();
		const ids = Array.from({ length: 21 }, (_, i) => String(i));
		const result = await inviteCoauthors(asApi({ POST }), 't1', ids);
		expect(isActionFailure(result)).toBe(true);
		expect(POST).not.toHaveBeenCalled();
	});

	it('returns the full invite list of the track', async () => {
		const POST = vi.fn(async () => ({
			data: [
				{ userId: 'u1', name: 'Ana', status: 'Pending' },
				{ userId: 'u2', name: 'Leo', status: 'Accepted' }
			]
		}));
		const result = await inviteCoauthors(asApi({ POST }), 't1', ['u1', 'u1']);
		expect(POST).toHaveBeenCalledWith('/tracks/{id}/coauthors', {
			params: { path: { id: 't1' } },
			body: { userIds: ['u1'] }
		});
		expect(result).toEqual({
			invites: [
				{ userId: 'u1', name: 'Ana', status: 'Pending' },
				{ userId: 'u2', name: 'Leo', status: 'Accepted' }
			]
		});
	});

	it.each([
		[400, 'ti mismo'],
		[403, 'autor'],
		[409, 'ya tiene']
	])('explains a %i response', async (status, text) => {
		const POST = vi.fn(async () => failed(status));
		const result = await inviteCoauthors(asApi({ POST }), 't1', ['u1']);
		expect(isActionFailure(result)).toBe(true);
		if (!isActionFailure(result)) return;
		expect(result.status).toBe(status);
		expect(result.data).toMatchObject({ message: expect.stringContaining(text) });
	});
});

describe('removeCoauthor', () => {
	it('returns the removed user', async () => {
		const DELETE = vi.fn(async () => ({}));
		expect(await removeCoauthor(asApi({ DELETE }), 't1', 'u1')).toEqual({ removed: 'u1' });
		expect(DELETE).toHaveBeenCalledWith('/tracks/{id}/coauthors/{userId}', {
			params: { path: { id: 't1', userId: 'u1' } }
		});
	});

	it('fails when the API refuses', async () => {
		const DELETE = vi.fn(async () => failed(403));
		expect(isActionFailure(await removeCoauthor(asApi({ DELETE }), 't1', 'u1'))).toBe(true);
	});
});

describe('respondToInvitation', () => {
	it('calls the endpoint matching the decision', async () => {
		const POST = vi.fn(async () => ({}));
		await respondToInvitation(asApi({ POST }), 'i1', 'accept');
		await respondToInvitation(asApi({ POST }), 'i1', 'reject');
		expect(POST).toHaveBeenNthCalledWith(1, '/coauthor-invitations/{id}/accept', {
			params: { path: { id: 'i1' } }
		});
		expect(POST).toHaveBeenNthCalledWith(2, '/coauthor-invitations/{id}/reject', {
			params: { path: { id: 'i1' } }
		});
	});

	it('reports an already answered invitation', async () => {
		const POST = vi.fn(async () => failed(409));
		const result = await respondToInvitation(asApi({ POST }), 'i1', 'accept');
		expect(isActionFailure(result)).toBe(true);
		if (isActionFailure(result)) expect(result.status).toBe(409);
	});
});

describe('loadInvitationCount', () => {
	it('reads the total of pending invitations', async () => {
		const GET = vi.fn(async () => ({ data: { items: [], totalItemCount: '3' } }));
		expect(await loadInvitationCount(asApi({ GET }))).toBe(3);
	});

	it('falls back to zero when the request fails', async () => {
		const GET = vi.fn(async () => failed(500));
		expect(await loadInvitationCount(asApi({ GET }))).toBe(0);
	});
});

describe('searchUsers', () => {
	it('keeps only what the invite UI needs', async () => {
		const GET = vi.fn(async () => ({
			data: {
				items: [
					{
						id: 'u1',
						name: 'Ana',
						firstName: 'A',
						secondName: null,
						profilePictureUrl: null,
						isFollowedByViewer: true
					}
				]
			}
		}));
		expect(await searchUsers(asApi({ GET }), 'an')).toEqual([
			{ id: 'u1', name: 'Ana', profilePictureUrl: null }
		]);
	});
});
