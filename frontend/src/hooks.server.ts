import type { Handle } from '@sveltejs/kit';
import {
	decodeSession,
	encodeSession,
	refreshSession,
	sessionCookieOptions,
	SESSION_COOKIE
} from '$lib/server/auth';

const REFRESH_THRESHOLD_SECONDS = 30;

export const handle: Handle = async ({ event, resolve }) => {
	let session = await decodeSession(event.cookies.get(SESSION_COOKIE));

	if (session && session.expiresAt - REFRESH_THRESHOLD_SECONDS <= Math.floor(Date.now() / 1000)) {
		session = await refreshSession(session);
		if (session) {
			event.cookies.set(SESSION_COOKIE, await encodeSession(session), sessionCookieOptions(event.url));
		} else {
			event.cookies.delete(SESSION_COOKIE, { path: '/' });
		}
	}

	if (session) {
		event.locals.user = {
			sub: session.sub,
			name: session.name,
			email: session.email,
			picture: session.picture
		};
		event.locals.accessToken = session.accessToken;
	} else {
		event.locals.user = null;
		event.locals.accessToken = null;
	}

	return resolve(event);
};
