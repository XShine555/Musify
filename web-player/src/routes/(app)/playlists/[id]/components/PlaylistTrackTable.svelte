<script lang="ts">
	import { enhance } from '$app/forms';
	import X from '@lucide/svelte/icons/x';
	import {
		player,
		isPendingYouTubeTrack,
		queueIdForTrack,
		toQueueItems,
		youTubeThumbnailUrl,
		type ApiTrackLike
	} from '$lib/player/player.svelte';
	import { fmtTime, fmtDate } from '$lib/format';
	import Badge from '$lib/components/ui/Badge.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';

	type PlaylistTrack = ApiTrackLike & { createdAt: string; duration: number | string };

	interface Props {
		tracks: PlaylistTrack[];
	}

	let { tracks }: Props = $props();

	function playFrom(index: number) {
		player.playOrToggle(toQueueItems(tracks), index);
	}
</script>

<TrackTable index action meta={[{ label: 'Añadida', width: '140px' }]} class="mt-6 sm:mt-8">
	{#each tracks as track, i (track.id)}
		<TrackRow>
			<TrackIndexCell
				index={i}
				active={player.current.id === queueIdForTrack(track)}
				playing={player.playing}
				onToggle={() => playFrom(i)}
			/>
			<TrackTitleCell
				trackId={track.id}
				title={track.title}
				artist={track.artist}
				coverSrc={isPendingYouTubeTrack(track) && track.externalId
					? youTubeThumbnailUrl(track.externalId)
					: undefined}
				explicit={track.isExplicit}
				onClick={() => playFrom(i)}
			>
				{#snippet badge()}
					{#if track.source === 'YouTube' && track.audioStatus === 'Failed'}
						<Badge tone="danger">Error</Badge>
					{:else if isPendingYouTubeTrack(track)}
						<Badge tone="accent">Descargando</Badge>
					{/if}
				{/snippet}
			</TrackTitleCell>
			<TrackMeta class="hidden sm:block">{fmtDate(track.createdAt)}</TrackMeta>
			<TrackMeta>{fmtTime(Number(track.duration))}</TrackMeta>
			<form
				method="POST"
				action="?/removeTrack"
				use:enhance={() =>
					async ({ update }) =>
						update()}
			>
				<input type="hidden" name="trackId" value={track.id} />
				<IconButton type="submit" label="Quitar de la playlist" size="sm" revealOnHover>
					<X class="h-3.5 w-3.5" />
				</IconButton>
			</form>
		</TrackRow>
	{/each}
</TrackTable>
