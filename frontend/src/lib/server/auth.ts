import * as client from 'openid-client';
import { EncryptJWT, jwtDecrypt } from 'jose';
import { env } from '$env/dynamic/private';
import type { SessionUser } from '$lib/types';

export const SESSION_COOKIE = 'mf_session';
export const STATE_COOKIE = 'mf_oidc_state';
export const VERIFIER_COOKIE = 'mf_oidc_verifier';
export const NONCE_COOKIE = 'mf_oidc_nonce';
export const RETURN_COOKIE = 'mf_oidc_return';

export interface Session extends SessionUser {
	accessToken: string;
	refreshToken?: string;
	idToken?: string;
	expiresAt: number;
}

export const OIDC_SCOPE = 'openid profile email offline_access';

let configPromise: Promise<client.Configuration> | null = null;

export function getOidcConfig(): Promise<client.Configuration> {
	configPromise ??= (async () => {
		const issuer = new URL(env.ZITADEL_ISSUER);
		const options =
			issuer.protocol === 'http:' ? { execute: [client.allowInsecureRequests] } : undefined;

		const secret = env.ZITADEL_CLIENT_SECRET;
		const hasSecret = !!secret && !secret.startsWith('REPLACE_');

		return hasSecret
			? client.discovery(issuer, env.ZITADEL_CLIENT_ID, secret, undefined, options)
			: client.discovery(issuer, env.ZITADEL_CLIENT_ID, undefined, client.None(), options);
	})();
	return configPromise;
}

export const authConfig = {
	get redirectUri() {
		return env.AUTH_REDIRECT_URI;
	},
	get postLogoutUri() {
		return env.AUTH_POST_LOGOUT_URI;
	}
};

let keyPromise: Promise<Uint8Array> | null = null;

function sessionKey(): Promise<Uint8Array> {
	keyPromise ??= crypto.subtle
		.digest('SHA-256', new TextEncoder().encode(env.SESSION_SECRET))
		.then((digest) => new Uint8Array(digest));
	return keyPromise;
}

export function sessionCookieOptions(url: URL) {
	return {
		httpOnly: true,
		sameSite: 'lax' as const,
		secure: url.protocol === 'https:',
		path: '/',
		maxAge: 60 * 60 * 24 * 7
	};
}

export async function encodeSession(session: Session): Promise<string> {
	return new EncryptJWT({ ...session })
		.setProtectedHeader({ alg: 'dir', enc: 'A256GCM' })
		.setIssuedAt()
		.setExpirationTime('7d')
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

export async function refreshSession(session: Session): Promise<Session | null> {
	if (!session.refreshToken) return null;
	try {
		const config = await getOidcConfig();
		const tokens = await client.refreshTokenGrant(config, session.refreshToken);
		const claims = tokens.claims();
		return {
			sub: session.sub,
			name: (claims?.name as string | undefined) ?? session.name,
			email: (claims?.email as string | undefined) ?? session.email,
			picture: (claims?.picture as string | undefined) ?? session.picture,
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
