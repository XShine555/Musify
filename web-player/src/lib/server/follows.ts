import type { ApiClient } from '$lib/server/api';
import type { FollowList } from '$lib/types';

export function fetchFollowList(
	api: ApiClient,
	list: FollowList,
	id: string,
	pageNumber: number,
	pageSize: number
) {
	const options = { params: { path: { id }, query: { pageNumber, pageSize } } };
	return list === 'followers'
		? api.GET('/users/{id}/followers', options)
		: api.GET('/users/{id}/following', options);
}
