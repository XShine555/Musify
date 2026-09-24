export interface PresignFile {
	fileName: string;
	contentType: string;
	size: number;
}

export interface PresignRequest {
	cover: PresignFile | null;
	files: PresignFile[];
}

export interface PresignTarget {
	uploadUrl: string;
	contentType: string;
}

export interface PresignedAudio extends PresignTarget {
	intentId: string;
	pictureIntentId: string | null;
}

export interface PresignResponse {
	picture: PresignTarget | null;
	audios: PresignedAudio[];
}

export interface CompleteRequest {
	title: string;
	audioIntentId: string;
	pictureIntentId: string | null;
	tags: string[];
	albumId: string | null;
}

export interface CompleteResponse {
	trackId: string;
	note: string | null;
}

export type TrackProcessing = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export type StatusResponse = Record<string, TrackProcessing>;
