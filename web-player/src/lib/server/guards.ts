import { redirect } from '@sveltejs/kit';
import type { SessionUser } from '$lib/types';

export function loginRedirect(url: URL): never {
	redirect(302, `/auth?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
}

export function requireUser(locals: App.Locals, url: URL): SessionUser {
	if (!locals.user) loginRedirect(url);
	return locals.user;
}

export function optionalUser(
	locals: App.Locals,
	url: URL,
	allowAnonymousListening: boolean
): SessionUser | null {
	if (locals.user) return locals.user;
	if (!allowAnonymousListening) loginRedirect(url);
	return null;
}
