import createClient from 'openapi-fetch';
import { error, fail, redirect, type ActionFailure, type RequestEvent } from '@sveltejs/kit';
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

export type ApiClient = ReturnType<typeof createApiClient>;

export function apiFor({ fetch, locals }: Pick<RequestEvent, 'fetch' | 'locals'>): ApiClient {
	return createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
}

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

export function formString(form: FormData, key: string): string {
	return String(form.get(key) ?? '');
}

export function formFile(form: FormData, key: string): File | null {
	const value = form.get(key);
	return value instanceof File && value.size > 0 ? value : null;
}

interface ActionContext {
	api: ApiClient;
	form: FormData;
	params: Record<string, string>;
	locals: App.Locals;
	url: URL;
}

export function authedAction<R>(
	handler: (context: ActionContext) => R | Promise<R>,
	unauthorizedMessage = 'Inicia sesión.'
) {
	return async (event: RequestEvent) => {
		if (!event.locals.accessToken) return fail(401, { message: unauthorizedMessage });
		const form = await event.request.formData();
		return handler({
			api: apiFor(event),
			form,
			params: event.params as Record<string, string>,
			locals: event.locals,
			url: event.url
		});
	};
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

type Failure = ActionFailure<{ message: string; detail?: string }>;

export function failOnError(result: { error?: unknown }, message: string): Failure | undefined {
	return result.error ? fail(502, { message, detail: apiErrorDetail(result.error) }) : undefined;
}

export function requireData<T>(
	result: { data?: T; error?: unknown },
	message: string
): { data: T; failure?: undefined } | { data?: undefined; failure: Failure } {
	if (result.error || result.data === undefined) {
		return { failure: fail(502, { message, detail: apiErrorDetail(result.error) }) };
	}
	return { data: result.data };
}
