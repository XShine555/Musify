<script lang="ts">
	import { enhance } from '$app/forms';
	import Plus from '@lucide/svelte/icons/plus';
	import { player, toQueueItems, type ApiTrackLike } from '$lib/player/player.svelte';
	import { hueFor } from '$lib/theme/color';
	import { fmtTime } from '$lib/format';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';

	type LibraryTrack = ApiTrackLike & { duration: number | string };

	interface Props {
		library: LibraryTrack[];
	}

	let { library }: Props = $props();

	function playFrom(index: number) {
		player.playOrToggle(toQueueItems(library), index);
	}
</script>

<div class="mt-10 sm:mt-12">
	<SectionHeading title="Tus canciones subidas" />
	<TrackTable columns="1fr 64px 36px" columnsMobile="1fr 52px 32px">
		{#snippet headers()}
			<span>Título</span>
			<span class="hidden text-center sm:block">Duración</span>
			<span></span>
		{/snippet}
		{#each library as track, i (track.id)}
			<TrackRow>
				<TrackTitleCell
					trackId={track.id}
					title={track.title}
					artist={track.artist}
					hue={hueFor(track.id)}
					explicit={track.isExplicit}
					onClick={() => playFrom(i)}
				>
					{#snippet overlay()}
						{#if player.current.id === track.id}
							<NowPlaying paused={!player.playing} />
						{/if}
					{/snippet}
				</TrackTitleCell>
				<TrackMeta>{fmtTime(Number(track.duration))}</TrackMeta>
				<form
					method="POST"
					action="?/addTrack"
					use:enhance={() =>
						async ({ update }) =>
							update()}
				>
					<input type="hidden" name="trackId" value={track.id} />
					<IconButton type="submit" label="Añadir al álbum" size="sm" revealOnHover>
						<Plus class="h-3.5 w-3.5" />
					</IconButton>
				</form>
			</TrackRow>
		{/each}
	</TrackTable>
</div>
