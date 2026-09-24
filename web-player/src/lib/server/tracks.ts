import { failOnError, type ApiClient } from '$lib/server/api';

const BULK_LIMIT = 100;

function chunks<T>(items: T[]): T[][] {
	const result: T[][] = [];
	for (let i = 0; i < items.length; i += BULK_LIMIT) result.push(items.slice(i, i + BULK_LIMIT));
	return result;
}

export async function deleteTracks(api: ApiClient, ids: string[]): Promise<{ failed: number }> {
	const results = await Promise.all(
		chunks(ids).map(async (batch) => {
			const result = await api.DELETE('/tracks', { body: { trackIds: batch } });
			return result.error || !result.data ? batch.length : result.data.failedTrackIds.length;
		})
	);
	return { failed: results.reduce((total, count) => total + count, 0) };
}

export async function setTracksPublished(
	api: ApiClient,
	ids: string[],
	isPublished: boolean
): Promise<{ updated: number; skipped: number } | NonNullable<ReturnType<typeof failOnError>>> {
	let updated = 0;
	for (const batch of chunks(ids)) {
		const result = await api.PUT('/tracks/published', { body: { trackIds: batch, isPublished } });
		const failure = failOnError(
			result,
			isPublished ? 'No se pudieron publicar las canciones.' : 'No se pudieron despublicar.'
		);
		if (failure) return failure;
		updated += result.data?.updatedTrackIds.length ?? 0;
	}
	return { updated, skipped: ids.length - updated };
}
