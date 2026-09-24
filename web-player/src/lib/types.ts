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

export interface GenreOption {
	genre: string;
	incompatibleWith: string[];
}

export type TrackStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export interface LibraryTrack extends Track {
	date: string;
	status: TrackStatus;
	genres: string[];
	albumTitle: string | null;
	albums: { id: string; title: string }[];
	published: boolean;
}

export interface TrackStatusCounts {
	total: number;
	processing: number;
	completed: number;
	failed: number;
	published: number;
}

export interface LikedTrack extends Track {
	likedAt: number;
}

export type AlbumType = 'Album' | 'EP' | 'Single';

export interface Album {
	id: string;
	title: string;
	description: string | null;
	releaseYear: number | null;
	type: AlbumType;
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

export interface ListeningStats {
	tracksThisWeek: number;
	secondsThisWeek: number;
	streakDays: number;
}

export interface Paged<T> {
	items: T[];
	pageNumber: number;
	hasNextPage: boolean;
	totalItemCount: number;
}
