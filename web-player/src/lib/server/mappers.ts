import type { components } from '$lib/api/schema';
import type { Album, LikedTrack, ListeningStats, Mix, Paged, Playlist, Track } from '$lib/types';

type TrackDto = components['schemas']['TrackApplicationResponse'];
type AlbumDto = components['schemas']['AlbumApplicationResponse'];
type PlaylistDto = components['schemas']['PlayListApplicationResponse'];
type MixDto = components['schemas']['MixApplicationResponse'];
type MixItemDto = components['schemas']['MixItemApplicationResponse'];

interface PageDto<T> {
	items: T[];
	pageNumber: number | string;
	hasNextPage: boolean;
	totalItemCount: number | string;
}

export function toTrack(dto: TrackDto): Track {
	return {
		id: dto.id,
		title: dto.title,
		artist: dto.artist,
		ownerUserId: dto.ownerUserId,
		explicit: dto.isExplicit,
		duration: Number(dto.duration) || 0,
		listensCount: Number(dto.listensCount) || 0
	};
}

export function toDatedTrack(dto: TrackDto): Track & { date: string } {
	return { ...toTrack(dto), date: dto.createdAt };
}

export function toLikedTrack(dto: TrackDto): LikedTrack {
	return { ...toTrack(dto), likedAt: new Date(dto.createdAt).getTime() };
}

export function toMixTrack(item: MixItemDto): Track {
	return {
		id: item.trackId,
		title: item.title,
		artist: item.artist,
		ownerUserId: null,
		explicit: false,
		duration: Number(item.durationSeconds) || 0,
		listensCount: Number(item.listensCount) || 0
	};
}

export function toMix(dto: MixDto): Mix {
	return {
		id: dto.id,
		title: dto.title,
		subtitle: dto.subtitle,
		tracks: dto.items.map(toMixTrack)
	};
}

export function toAlbum(dto: AlbumDto): Album {
	return {
		id: dto.id,
		title: dto.title,
		description: dto.description,
		releaseYear: dto.releaseYear === null ? null : Number(dto.releaseYear),
		ownerUserId: dto.ownerUserId,
		trackCount: Number(dto.trackCount) || 0,
		coverTrackIds: dto.coverTrackIds,
		updatedAt: dto.updatedAt
	};
}

export function toPlaylist(dto: PlaylistDto): Playlist {
	return {
		id: dto.id,
		name: dto.name,
		description: dto.description,
		visibility: dto.visibility,
		ownerUserId: dto.ownerUserId,
		trackCount: Number(dto.trackCount) || 0,
		durationSeconds: Number(dto.durationSeconds) || 0,
		coverTrackIds: dto.coverTrackIds,
		updatedAt: dto.updatedAt
	};
}

type ListeningStatsDto = components['schemas']['ListeningStatsResponse'];

export function toCount(value: number | string | null | undefined): number {
	return Number(value ?? 0) || 0;
}

export function toListeningStats(dto: ListeningStatsDto | null | undefined): ListeningStats {
	return {
		tracksThisWeek: toCount(dto?.tracksThisWeek),
		secondsThisWeek: toCount(dto?.secondsThisWeek),
		streakDays: toCount(dto?.streakDays)
	};
}

export function toPage<D, T>(dto: PageDto<D>, map: (item: D) => T): Paged<T> {
	return {
		items: dto.items.map(map),
		pageNumber: Number(dto.pageNumber) || 1,
		hasNextPage: dto.hasNextPage,
		totalItemCount: Number(dto.totalItemCount) || 0
	};
}
