import { describe, expect, it } from 'vitest';
import { loginRedirect, optionalUser, requireUser } from './guards';

const user = { sub: 'u1', name: 'Ana' };

function redirectOf(fn: () => unknown): { status: number; location: string } {
	try {
		fn();
	} catch (thrown) {
		return thrown as { status: number; location: string };
	}
	throw new Error('Expected a redirect');
}

describe('loginRedirect', () => {
	it('redirects to /auth with the encoded path and query', () => {
		const result = redirectOf(() => loginRedirect(new URL('http://x/albums/1?tab=a&b=c')));
		expect(result.status).toBe(302);
		expect(result.location).toBe(`/auth?returnTo=${encodeURIComponent('/albums/1?tab=a&b=c')}`);
	});
});

describe('requireUser', () => {
	it('returns the user when signed in', () => {
		const locals = { user, accessToken: 't' } as App.Locals;
		expect(requireUser(locals, new URL('http://x/'))).toBe(user);
	});

	it('redirects when there is no user', () => {
		const locals = { user: null, accessToken: null } as App.Locals;
		const result = redirectOf(() => requireUser(locals, new URL('http://x/library')));
		expect(result.location).toBe('/auth?returnTo=%2Flibrary');
	});
});

describe('optionalUser', () => {
	it('returns the user when signed in', () => {
		const locals = { user, accessToken: 't' } as App.Locals;
		expect(optionalUser(locals, new URL('http://x/'), false)).toBe(user);
	});

	it('returns null for anonymous listening', () => {
		const locals = { user: null, accessToken: null } as App.Locals;
		expect(optionalUser(locals, new URL('http://x/'), true)).toBeNull();
	});

	it('redirects anonymous visitors when listening is not allowed', () => {
		const locals = { user: null, accessToken: null } as App.Locals;
		const result = redirectOf(() => optionalUser(locals, new URL('http://x/albums/1'), false));
		expect(result.location).toBe('/auth?returnTo=%2Falbums%2F1');
	});
});
