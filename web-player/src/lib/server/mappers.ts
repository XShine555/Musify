import type { components } from '$lib/api/schema';
import type {
	Album,
	CoauthorInvitation,
	CoauthorInvite,
	LibraryTrack,
	TrackStatusCounts,
	LikedTrack,
	ListeningStats,
	Mix,
	Paged,
	Playlist,
	Track
} from '$lib/types';

type TrackDto = components['schemas']['TrackApplicationResponse'];
type UserTrackDto = components['schemas']['UserTrackApplicationResponse'];
type CoauthorInviteDto = components['schemas']['TrackCoauthorInviteReference'];
type CoauthorInvitationDto = components['schemas']['CoauthorInvitationResponse'];
type StatusCountsDto = components['schemas']['TrackStatusCountsResponse'];
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
		listensCount: Number(dto.listensCount) || 0,
		coauthors: dto.coauthors.map(({ id, name }) => ({ id, name }))
	};
}

export function toDatedTrack(dto: TrackDto): Track & { date: string } {
	return { ...toTrack(dto), date: dto.createdAt };
}

export function toCoauthorInvite({ userId, name, status }: CoauthorInviteDto): CoauthorInvite {
	return { userId, name, status };
}

export function toCoauthorInvitation(dto: CoauthorInvitationDto): CoauthorInvitation {
	return {
		id: dto.id,
		trackId: dto.trackId,
		trackTitle: dto.trackTitle,
		artist: dto.artist,
		ownerUserId: dto.ownerUserId,
		invitedAt: dto.invitedAt
	};
}

export function toLibraryTrack(dto: UserTrackDto): LibraryTrack {
	return {
		...toDatedTrack(dto),
		status: dto.audioStatus,
		genres: dto.tags,
		albums: dto.albums,
		albumTitle: dto.albums.length > 0 ? dto.albums.map((album) => album.title).join(', ') : null,
		published: dto.isPublished,
		coauthorInvites: dto.coauthorInvites.map(toCoauthorInvite)
	};
}

export function toStatusCounts(dto: StatusCountsDto): TrackStatusCounts {
	return {
		total: Number(dto.total) || 0,
		processing: (Number(dto.pending) || 0) + (Number(dto.processing) || 0),
		completed: Number(dto.completed) || 0,
		failed: Number(dto.failed) || 0,
		published: Number(dto.published) || 0
	};
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
		listensCount: Number(item.listensCount) || 0,
		coauthors: []
	};
}

const MIX_TEXTS: Record<MixDto['kind'], { title: string; subtitle: string }> = {
	Discovery: {
		title: 'Descubrimiento',
		subtitle: 'Canciones de tu biblioteca que todavía no has escuchado.'
	},
	Daily: {
		title: 'Tu mezcla diaria',
		subtitle: 'Lo que más escuchas, con alguna sorpresa de tu biblioteca.'
	}
};

export function toMix(dto: MixDto): Mix {
	return {
		id: dto.id,
		...MIX_TEXTS[dto.kind],
		tracks: dto.items.map(toMixTrack)
	};
}

export function toAlbum(dto: AlbumDto): Album {
	return {
		id: dto.id,
		title: dto.title,
		description: dto.description,
		releaseYear: dto.releaseYear === null ? null : Number(dto.releaseYear),
		type: dto.type,
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
