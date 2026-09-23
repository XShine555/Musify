import type { Cookies } from '@sveltejs/kit';
import * as client from 'openid-client';
import { EncryptJWT, jwtDecrypt } from 'jose';
import { dev } from '$app/environment';
import { authConfig } from '$lib/server/config';
import type { SessionUser } from '$lib/types';

export const SESSION_COOKIE = 'mf_session';
export const STATE_COOKIE = 'mf_oidc_state';
export const VERIFIER_COOKIE = 'mf_oidc_verifier';
export const NONCE_COOKIE = 'mf_oidc_nonce';
export const RETURN_COOKIE = 'mf_oidc_return';

export interface Session extends SessionUser {
	accessToken: string;
	refreshToken: string;
	idToken: string;
	expiresAt: number;
}

let configPromise: Promise<client.Configuration> | undefined;
export function getOidcConfig(): Promise<client.Configuration> {
	configPromise ??= (async () => {
		const issuer = new URL(authConfig.issuer);
		const insecureAllowed = (dev || authConfig.allowInsecureHttp) && issuer.protocol === 'http:';
		const options = insecureAllowed ? { execute: [client.allowInsecureRequests] } : undefined;

		const secret = authConfig.clientSecret;
		return secret
			? client.discovery(issuer, authConfig.clientId, secret, undefined, options)
			: client.discovery(issuer, authConfig.clientId, undefined, client.None(), options);
	})().catch((exception) => {
		configPromise = undefined;
		throw exception;
	});
	return configPromise;
}

let keyPromise: Promise<Uint8Array>;
function sessionKey(): Promise<Uint8Array> {
	keyPromise ??= crypto.subtle
		.digest('SHA-256', new TextEncoder().encode(authConfig.sessionSecret))
		.then((digest) => new Uint8Array(digest));
	return keyPromise;
}

export function sessionCookieOptions(url: URL) {
	return {
		httpOnly: true,
		sameSite: 'lax' as const,
		secure: url.protocol === 'https:',
		path: '/',
		maxAge: authConfig.sessionTtlSeconds
	};
}

const SESSION_CHUNK_SIZE = 3600;

function sessionChunkName(index: number): string {
	return index === 0 ? SESSION_COOKIE : `${SESSION_COOKIE}.${index}`;
}

export function readSessionCookie(cookies: Cookies): string | undefined {
	const parts: string[] = [];
	for (let index = 0; ; index++) {
		const part = cookies.get(sessionChunkName(index));
		if (part === undefined) break;
		parts.push(part);
	}
	return parts.length > 0 ? parts.join('') : undefined;
}

export function clearSessionCookie(cookies: Cookies, keep = 0): void {
	for (const { name } of cookies.getAll()) {
		const match =
			name === SESSION_COOKIE
				? 0
				: name.startsWith(`${SESSION_COOKIE}.`)
					? Number(name.slice(SESSION_COOKIE.length + 1))
					: -1;
		if (Number.isInteger(match) && match >= keep) cookies.delete(name, { path: '/' });
	}
}

export function writeSessionCookie(cookies: Cookies, token: string, url: URL): void {
	const options = sessionCookieOptions(url);
	const count = Math.ceil(token.length / SESSION_CHUNK_SIZE);
	for (let index = 0; index < count; index++) {
		const chunk = token.slice(index * SESSION_CHUNK_SIZE, (index + 1) * SESSION_CHUNK_SIZE);
		cookies.set(sessionChunkName(index), chunk, options);
	}
	clearSessionCookie(cookies, count);
}

export async function encodeSession(session: Session): Promise<string> {
	const now = Math.floor(Date.now() / 1000);
	return new EncryptJWT({ ...session })
		.setProtectedHeader({ alg: 'dir', enc: 'A256GCM' })
		.setIssuedAt(now)
		.setExpirationTime(now + authConfig.sessionTtlSeconds)
		.encrypt(await sessionKey());
}

export async function decodeSession(token: string | undefined): Promise<Session | null> {
	if (!token) return null;
	try {
		const { payload } = await jwtDecrypt(token, await sessionKey());
		return payload as unknown as Session;
	} catch {
		return null;
	}
}

function claimString(value: unknown): string | undefined {
	return typeof value === 'string' && value.length > 0 ? value : undefined;
}

export function toSessionUser(
	claims: Record<string, unknown>,
	fallback?: SessionUser
): SessionUser {
	const sub = claimString(claims.sub) ?? fallback?.sub ?? '';
	return {
		sub,
		name:
			claimString(claims.name) ??
			claimString(claims.preferred_username) ??
			fallback?.name ??
			claimString(claims.email) ??
			sub,
		email: claimString(claims.email) ?? fallback?.email ?? '',
		picture: claimString(claims.picture) ?? fallback?.picture ?? ''
	};
}

export async function refreshSession(session: Session): Promise<Session | null> {
	try {
		const config = await getOidcConfig();
		const tokens = await client.refreshTokenGrant(config, session.refreshToken);
		return {
			...toSessionUser(tokens.claims() ?? {}, session),
			accessToken: tokens.access_token,
			refreshToken: tokens.refresh_token ?? session.refreshToken,
			idToken: tokens.id_token ?? session.idToken,
			expiresAt: Math.floor(Date.now() / 1000) + (tokens.expires_in ?? 3600)
		};
	} catch (exception) {
		console.error('Token refresh failed', exception);
		return null;
	}
}
