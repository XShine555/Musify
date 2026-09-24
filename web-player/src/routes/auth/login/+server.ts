import { redirect } from '@sveltejs/kit';
import * as client from 'openid-client';
import type { RequestHandler } from './$types';
import {
	getOidcConfig,
	safeReturnTo,
	STATE_COOKIE,
	VERIFIER_COOKIE,
	NONCE_COOKIE,
	RETURN_COOKIE
} from '$lib/server/auth';
import { authConfig } from '$lib/server/config';

export const GET: RequestHandler = async ({ url, cookies }) => {
	const config = await getOidcConfig();

	const codeVerifier = client.randomPKCECodeVerifier();
	const codeChallenge = await client.calculatePKCECodeChallenge(codeVerifier);
	const state = client.randomState();
	const nonce = client.randomNonce();

	const register = url.searchParams.get('mode') === 'register';
	const returnTo = safeReturnTo(url.searchParams.get('returnTo'));

	const parameters: Record<string, string> = {
		redirect_uri: authConfig.redirectUri,
		scope: authConfig.scope,
		code_challenge: codeChallenge,
		code_challenge_method: 'S256',
		state,
		nonce
	};
	if (register) parameters.prompt = 'create';

	const authorizationUrl = client.buildAuthorizationUrl(config, parameters);

	const cookieOptions = {
		httpOnly: true,
		sameSite: 'lax' as const,
		secure: url.protocol === 'https:',
		path: '/',
		maxAge: authConfig.flowTtlSeconds
	};
	cookies.set(VERIFIER_COOKIE, codeVerifier, cookieOptions);
	cookies.set(STATE_COOKIE, state, cookieOptions);
	cookies.set(NONCE_COOKIE, nonce, cookieOptions);
	cookies.set(RETURN_COOKIE, returnTo, cookieOptions);

	redirect(302, authorizationUrl.href);
};
