import createClient from 'openapi-fetch';
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
