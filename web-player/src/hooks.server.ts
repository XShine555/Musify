import type { Handle } from '@sveltejs/kit';
import {
	clearSessionCookie,
	decodeSession,
	encodeSession,
	readSessionCookie,
	refreshSession,
	writeSessionCookie
} from '$lib/server/auth';
import { authConfig } from '$lib/server/config';

export const handle: Handle = async ({ event, resolve }) => {
	let session = await decodeSession(readSessionCookie(event.cookies));

	if (
		session &&
		session.expiresAt - authConfig.refreshThresholdSeconds <= Math.floor(Date.now() / 1000)
	) {
		session = await refreshSession(session);
		if (session) {
			writeSessionCookie(event.cookies, await encodeSession(session), event.url);
		} else {
			clearSessionCookie(event.cookies);
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
