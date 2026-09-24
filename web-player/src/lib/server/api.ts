import createClient from 'openapi-fetch';
import { error, fail, type ActionFailure, type RequestEvent } from '@sveltejs/kit';
import type { paths } from '$lib/api/schema';
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
