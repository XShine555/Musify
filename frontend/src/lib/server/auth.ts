import * as client from 'openid-client';
import { SignJWT, jwtVerify } from 'jose';
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

const sessionKey = () => new TextEncoder().encode(env.SESSION_SECRET);

export async function encodeSession(session: Session): Promise<string> {
	return new SignJWT({ ...session })
		.setProtectedHeader({ alg: 'HS256' })
		.setIssuedAt()
		.setExpirationTime('7d')
		.sign(sessionKey());
}

export async function decodeSession(token: string | undefined): Promise<Session | null> {
	if (!token) return null;
	try {
		const { payload } = await jwtVerify(token, sessionKey());
		return payload as unknown as Session;
	} catch {
		return null;
	}
}
