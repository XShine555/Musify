export interface SessionUser {
	sub: string;
	name: string;
	email: string;
	picture: string;
}

export type FollowList = 'followers' | 'following';
