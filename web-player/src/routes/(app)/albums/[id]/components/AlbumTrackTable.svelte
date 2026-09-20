<script lang="ts">
	import { enhance } from '$app/forms';
	import X from '@lucide/svelte/icons/x';
	import { player, toQueueItems, type ApiTrackLike } from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/format';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';

	type AlbumTrack = ApiTrackLike & { duration: number | string; listensCount: number | string };

	interface Props {
		tracks: AlbumTrack[];
		isOwner: boolean;
	}

	let { tracks, isOwner }: Props = $props();

	function playFrom(index: number) {
		player.playOrToggle(toQueueItems(tracks), index);
	}
</script>

<TrackTable
	index
	action={isOwner}
	meta={[{ label: 'Escuchas', width: '100px' }]}
	class="mt-6 sm:mt-8"
>
	{#each tracks as track, i (track.id)}
		{@const active = player.current.id === track.id}
		<TrackRow {active}>
			<TrackIndexCell index={i} {active} playing={player.playing} onToggle={() => playFrom(i)} />
			<TrackTitleCell
				trackId={track.id}
				title={track.title}
				artist={track.artist}
				ownerUserId={track.ownerUserId}
				explicit={track.isExplicit}
				{active}
				onClick={() => playFrom(i)}
			/>
			<TrackMeta class="hidden sm:block">{Number(track.listensCount)}</TrackMeta>
			<TrackMeta>{fmtTime(Number(track.duration))}</TrackMeta>
			{#if isOwner}
				<form
					method="POST"
					action="?/removeTrack"
					use:enhance={() =>
						async ({ update }) =>
							update()}
				>
					<input type="hidden" name="trackId" value={track.id} />
					<IconButton type="submit" label="Quitar del álbum" size="xs" revealOnHover>
						<X class="h-3.5 w-3.5" />
					</IconButton>
				</form>
			{/if}
		</TrackRow>
	{/each}
</TrackTable>
