import createClient from 'openapi-fetch';
import { error, fail, redirect, type ActionFailure } from '@sveltejs/kit';
import type { paths } from '$lib/api/schema';
import type { SessionUser } from '$lib/types';
import { apiConfig } from '$lib/server/config';

interface ApiClientOptions {
	fetch: typeof fetch;
	accessToken?: string;
}

export function createApiClient({ fetch, accessToken }: ApiClientOptions) {
	return createClient<paths>({
		baseUrl: apiConfig.baseUrl,
		fetch,
		headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined
	});
}

export function requireUser(locals: App.Locals, url: URL): SessionUser {
	if (!locals.user)
		redirect(302, `/auth?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
	return locals.user;
}

export function optionalUser(
	locals: App.Locals,
	url: URL,
	allowAnonymousListening: boolean
): SessionUser | null {
	if (locals.user) return locals.user;
	if (!allowAnonymousListening)
		redirect(302, `/auth?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
	return null;
}

export function requireAccessTokenAction(
	locals: App.Locals,
	message = 'Inicia sesión.'
): string | ActionFailure<{ message: string }> {
	return locals.accessToken ?? fail(401, { message });
}

export function unwrapOrError<T>(
	result: { data?: T; error?: unknown; response?: Response },
	message: string,
	status?: number
): T {
	if (result.error || result.data === undefined) {
		error(status ?? result.response?.status ?? 502, message);
	}
	return result.data;
}

export function apiErrorDetail(error: unknown): string | undefined {
	if (typeof error !== 'object' || error === null) return undefined;
	const { errors, detail, title } = error as {
		errors?: Record<string, unknown>;
		detail?: unknown;
		title?: unknown;
	};
	if (errors && typeof errors === 'object') {
		const messages = Object.values(errors)
			.flat()
			.filter((entry): entry is string => typeof entry === 'string');
		if (messages.length > 0) return messages.join(' ');
	}
	if (typeof detail === 'string' && detail !== '') return detail;
	if (typeof title === 'string' && title !== '') return title;
	return undefined;
}

export function unwrapOrFail(
	result: { error?: unknown },
	message: string
): ActionFailure<{ message: string; detail?: string }> | undefined {
	return result.error ? fail(502, { message, detail: apiErrorDetail(result.error) }) : undefined;
}
