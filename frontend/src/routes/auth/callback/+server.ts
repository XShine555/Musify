import { redirect, error } from '@sveltejs/kit';
import * as client from 'openid-client';
import type { RequestHandler } from './$types';
import {
	getOidcConfig,
	encodeSession,
	sessionCookieOptions,
	type Session,
	SESSION_COOKIE,
	STATE_COOKIE,
	VERIFIER_COOKIE,
	NONCE_COOKIE,
	RETURN_COOKIE
} from '$lib/server/auth';

export const GET: RequestHandler = async ({ url, cookies }) => {
	const codeVerifier = cookies.get(VERIFIER_COOKIE);
	const state = cookies.get(STATE_COOKIE);
	const nonce = cookies.get(NONCE_COOKIE);
	const returnTo = cookies.get(RETURN_COOKIE) ?? '/';

	if (!codeVerifier || !state) {
		error(400, 'Estado de autenticación inválido. Vuelve a iniciar sesión.');
	}

	const config = await getOidcConfig();

	let tokens;
	try {
		tokens = await client.authorizationCodeGrant(config, url, {
			pkceCodeVerifier: codeVerifier,
			expectedState: state,
			expectedNonce: nonce,
			idTokenExpected: true
		});
	} catch (exception) {
		console.error('Token exchange failed', exception);
		error(401, 'La autenticación ha fallado.');
	}

	const claims = tokens.claims();
	if (!claims) {
		error(401, 'El proveedor no devolvió el token de identidad.');
	}

	const session: Session = {
		sub: String(claims.sub),
		name: claims.name as string | undefined,
		email: claims.email as string | undefined,
		picture: claims.picture as string | undefined,
		accessToken: tokens.access_token,
		refreshToken: tokens.refresh_token,
		idToken: tokens.id_token,
		expiresAt: Math.floor(Date.now() / 1000) + (tokens.expires_in ?? 3600)
	};

	cookies.set(SESSION_COOKIE, await encodeSession(session), sessionCookieOptions(url));

	for (const name of [STATE_COOKIE, VERIFIER_COOKIE, NONCE_COOKIE, RETURN_COOKIE]) {
		cookies.delete(name, { path: '/' });
	}

	redirect(302, returnTo);
};
