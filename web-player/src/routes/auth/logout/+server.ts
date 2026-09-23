import { redirect } from '@sveltejs/kit';
import * as client from 'openid-client';
import type { RequestHandler } from './$types';
import {
	clearSessionCookie,
	decodeSession,
	getOidcConfig,
	readSessionCookie
} from '$lib/server/auth';
import { authConfig } from '$lib/server/config';

const handleLogout: RequestHandler = async ({ cookies }) => {
	const session = await decodeSession(readSessionCookie(cookies));
	clearSessionCookie(cookies);

	const config = await getOidcConfig();
	const endSessionUrl = client.buildEndSessionUrl(config, {
		post_logout_redirect_uri: authConfig.postLogoutUri,
		...(session?.idToken ? { id_token_hint: session.idToken } : {})
	});

	redirect(302, endSessionUrl.href);
};

export const GET = handleLogout;
export const POST = handleLogout;
