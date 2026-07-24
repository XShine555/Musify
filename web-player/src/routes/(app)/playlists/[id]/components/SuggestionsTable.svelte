<script lang="ts">
	import { enhance } from '$app/forms';
	import Plus from '@lucide/svelte/icons/plus';
	import {
		player,
		queueIdForTrack,
		type ApiTrackLike,
		type QueueItem
	} from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/format';
	import type { YouTubeSong } from '$lib/types';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';

	type LibraryTrack = ApiTrackLike & { duration: number | string };
	type Suggestion = { kind: 'local'; track: LibraryTrack } | { kind: 'youtube'; song: YouTubeSong };

	interface Props {
		library: LibraryTrack[];
		youtube: YouTubeSong[];
		playlists: { id: string; name: string }[];
	}

	let { library, youtube, playlists }: Props = $props();

	let contextMenu = $state<ContextMenuState | null>(null);

	function openContextMenu(event: MouseEvent, item: Suggestion) {
		contextMenu = contextMenuStateFor(event, item);
	}

	function closeContextMenu() {
		contextMenu = null;
	}

	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemFor(contextMenu as Suggestion));
		closeContextMenu();
	}

	const suggestions = $derived<Suggestion[]>([
		...library.map((track) => ({ kind: 'local' as const, track })),
		...youtube.map((song) => ({ kind: 'youtube' as const, song }))
	]);

	function idFor(item: Suggestion) {
		return item.kind === 'youtube' ? item.song.videoId : queueIdForTrack(item.track);
	}

	function queueItemFor(item: Suggestion): QueueItem {
		return item.kind === 'youtube'
			? {
					id: item.song.videoId,
					title: item.song.title,
					artist: item.song.artist,
					source: 'youtube',
					coverUrl: item.song.thumbnailUrl,
					explicit: item.song.isExplicit
				}
			: {
					id: queueIdForTrack(item.track),
					title: item.track.title,
					artist: item.track.artist ?? undefined,
					source: 'local',
					coverUrl: `/api/tracks/${item.track.id}/cover?size=small`
				};
	}

	function playFrom(index: number) {
		player.playOrToggle(suggestions.map(queueItemFor), index);
	}
</script>

<div class="mt-10 sm:mt-12">
	<SectionHeading title="Sugerencias" />
	<TrackTable action>
		{#each suggestions as item, i (idFor(item))}
			<TrackRow oncontextmenu={(e) => openContextMenu(e, item)}>
				<TrackTitleCell
					trackId={item.kind === 'youtube' ? item.song.videoId : item.track.id}
					title={item.kind === 'youtube' ? item.song.title : item.track.title}
					artist={item.kind === 'youtube' ? item.song.artist : item.track.artist}
					coverSrc={item.kind === 'youtube' ? item.song.thumbnailUrl : undefined}
					explicit={item.kind === 'youtube' && item.song.isExplicit}
					onClick={() => playFrom(i)}
				>
					{#snippet overlay()}
						{#if player.current.id === idFor(item)}
							<NowPlaying paused={!player.playing} />
						{/if}
					{/snippet}
				</TrackTitleCell>
				<TrackMeta>
					{fmtTime(
						item.kind === 'youtube' ? item.song.durationSeconds : Number(item.track.duration)
					)}
				</TrackMeta>
				{#if item.kind === 'youtube'}
					<form
						method="POST"
						action="?/addYouTubeToPlaylist"
						use:enhance={() =>
							async ({ update }) =>
								update()}
					>
						<input type="hidden" name="videoId" value={item.song.videoId} />
						<input type="hidden" name="title" value={item.song.title} />
						<input type="hidden" name="artist" value={item.song.artist} />
						<input type="hidden" name="durationSeconds" value={item.song.durationSeconds} />
						<input type="hidden" name="thumbnailUrl" value={item.song.thumbnailUrl} />
						<IconButton type="submit" label="Añadir a la playlist" size="sm">
							<Plus class="h-4 w-4" />
						</IconButton>
					</form>
				{:else}
					<form
						method="POST"
						action="?/addTrack"
						use:enhance={() =>
							async ({ update }) =>
								update()}
					>
						<input type="hidden" name="trackId" value={item.track.id} />
						<IconButton type="submit" label="Añadir a la playlist" size="sm">
							<Plus class="h-4 w-4" />
						</IconButton>
					</form>
				{/if}
			</TrackRow>
		{/each}
	</TrackTable>
</div>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		{playlists}
		onClose={closeContextMenu}
		onAddToQueue={addToQueue}
	/>
{/if}
