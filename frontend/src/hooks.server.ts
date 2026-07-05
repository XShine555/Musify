import type { Handle } from '@sveltejs/kit';
import { decodeSession, SESSION_COOKIE } from '$lib/server/auth';

export const handle: Handle = async ({ event, resolve }) => {
	const session = await decodeSession(event.cookies.get(SESSION_COOKIE));

	if (session && session.expiresAt * 1000 > Date.now()) {
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
