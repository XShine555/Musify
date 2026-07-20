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
