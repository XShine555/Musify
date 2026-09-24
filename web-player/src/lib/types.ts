export interface SessionUser {
	sub: string;
	name: string;
	email: string;
	picture: string;
}

export type FollowList = 'followers' | 'following';

export interface Track {
	id: string;
	title: string;
	artist: string | null;
	ownerUserId: string | null;
	explicit: boolean;
	duration: number;
	listensCount: number;
}

export interface LikedTrack extends Track {
	likedAt: number;
}

export interface Album {
	id: string;
	title: string;
	description: string | null;
	releaseYear: number | null;
	ownerUserId: string;
	trackCount: number;
	coverTrackIds: string[];
	updatedAt: string;
}

export interface Playlist {
	id: string;
	name: string;
	description: string | null;
	visibility: 'Public' | 'Private';
	ownerUserId: string;
	trackCount: number;
	durationSeconds: number;
	coverTrackIds: string[];
	updatedAt: string;
}

export interface Mix {
	id: string;
	title: string;
	subtitle: string | null;
	tracks: Track[];
}

export interface Paged<T> {
	items: T[];
	pageNumber: number;
	hasNextPage: boolean;
	totalItemCount: number;
}
