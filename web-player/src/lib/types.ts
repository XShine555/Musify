export interface SessionUser {
	sub: string;
	name: string;
	email: string;
	picture: string;
}

export interface YouTubeSong {
	videoId: string;
	title: string;
	artist: string;
	album: string;
	durationSeconds: number;
	thumbnailUrl: string;
	isExplicit: boolean;
}

export interface YouTubeAlbumResult {
	albumId: string;
	title: string;
	artist: string;
	thumbnailUrl: string;
	releaseYear: number | string | null;
}
