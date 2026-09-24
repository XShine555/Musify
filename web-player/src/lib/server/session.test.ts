import { describe, expect, it, vi } from 'vitest';
import { EncryptJWT } from 'jose';

const testEnv = vi.hoisted(() => ({ SESSION_SECRET: 'a'.repeat(40) }) as Record<string, string>);

vi.mock('$env/dynamic/private', () => ({ env: testEnv }));

import { decodeSession, encodeSession, type Session } from './auth';

const session: Session = {
	sub: 'user-1',
	name: 'Test User',
	email: 'test@example.com',
	accessToken: 'access',
	refreshToken: 'refresh',
	idToken: 'id',
	expiresAt: 1_900_000_000
};

async function encryptWith(secret: string, ttlSeconds: number): Promise<string> {
	const now = Math.floor(Date.now() / 1000);
	const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(secret));
	return new EncryptJWT({ ...session })
		.setProtectedHeader({ alg: 'dir', enc: 'A256GCM' })
		.setIssuedAt(now)
		.setExpirationTime(now + ttlSeconds)
		.encrypt(new Uint8Array(digest));
}

describe('encodeSession / decodeSession', () => {
	it('round-trips a session', async () => {
		const token = await encodeSession(session);
		const decoded = await decodeSession(token);
		expect(decoded).toMatchObject(session);
	});

	it('returns null for a missing or empty token', async () => {
		expect(await decodeSession(undefined)).toBeNull();
		expect(await decodeSession('')).toBeNull();
	});

	it('returns null for a tampered token', async () => {
		const token = await encodeSession(session);
		const tampered = token.slice(0, -4) + (token.endsWith('AAAA') ? 'BBBB' : 'AAAA');
		expect(await decodeSession(tampered)).toBeNull();
		expect(await decodeSession('not.a.token')).toBeNull();
	});

	it('returns null for a token encrypted with another key', async () => {
		const token = await encryptWith('b'.repeat(40), 3600);
		expect(await decodeSession(token)).toBeNull();
	});

	it('returns null for an expired token', async () => {
		const token = await encryptWith(testEnv.SESSION_SECRET, -50);
		expect(await decodeSession(token)).toBeNull();
	});

	it('accepts a token built with the configured key', async () => {
		const token = await encryptWith(testEnv.SESSION_SECRET, 3600);
		expect(await decodeSession(token)).toMatchObject({ sub: 'user-1' });
	});
});
