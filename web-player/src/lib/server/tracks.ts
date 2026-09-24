import type { ApiClient } from '$lib/server/api';

export async function deleteTracks(api: ApiClient, ids: string[]): Promise<{ failed: number }> {
	const results = await Promise.all(
		ids.map((id) => api.DELETE('/tracks/{id}', { params: { path: { id } } }))
	);
	return { failed: results.filter((result) => result.error).length };
}
