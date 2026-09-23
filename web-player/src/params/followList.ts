import type { ParamMatcher } from '@sveltejs/kit';
import type { FollowList } from '$lib/types';

export const match = ((param: string): param is FollowList =>
	param === 'followers' || param === 'following') satisfies ParamMatcher;
