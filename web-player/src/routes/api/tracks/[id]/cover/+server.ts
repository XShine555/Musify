import type { RequestHandler } from './$types';
import { proxyCoverImage } from '$lib/server/image';

export const GET: RequestHandler = async ({ params, url, fetch }) => {
	return proxyCoverImage(fetch, 'tracks', params.id, url.searchParams.get('size'));
};
