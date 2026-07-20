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
	if (!locals.user) redirect(302, `/login?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
	return locals.user;
}

export function requireAccessToken(locals: App.Locals, message = 'Inicia sesión.'): string {
	if (!locals.accessToken) error(401, message);
	return locals.accessToken;
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

export function unwrapOrFail(
	result: { error?: unknown },
	message: string
): ActionFailure<{ message: string }> | undefined {
	return result.error ? fail(502, { message }) : undefined;
}

export async function playlistCoverTrackIds(
	api: ReturnType<typeof createApiClient>,
	playlistIds: string[]
): Promise<Record<string, string[]>> {
	const covers = await Promise.all(
		playlistIds.map(async (playlistId) => {
			const { data: tracks } = await api.GET('/playlists/{playlistId}/tracks', {
				params: { path: { playlistId }, query: { pageNumber: 1, pageSize: 4 } }
			});
			return [playlistId, tracks?.items.map((t) => t.id) ?? []] as const;
		})
	);
	return Object.fromEntries(covers);
}
