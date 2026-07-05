import { redirect } from '@sveltejs/kit';
import * as client from 'openid-client';
import type { RequestHandler } from './$types';
import { getOidcConfig, authConfig, decodeSession, SESSION_COOKIE } from '$lib/server/auth';

const handleLogout: RequestHandler = async ({ cookies }) => {
	const session = await decodeSession(cookies.get(SESSION_COOKIE));
	cookies.delete(SESSION_COOKIE, { path: '/' });

	const config = await getOidcConfig();
	const endSessionUrl = client.buildEndSessionUrl(config, {
		post_logout_redirect_uri: authConfig.postLogoutUri,
		...(session?.idToken ? { id_token_hint: session.idToken } : {})
	});

	redirect(302, endSessionUrl.href);
};

export const GET = handleLogout;
export const POST = handleLogout;
