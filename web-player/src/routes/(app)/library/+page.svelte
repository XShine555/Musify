<script lang="ts">
	import { enhance } from '$app/forms';
	import Upload from '@lucide/svelte/icons/upload';
	import X from '@lucide/svelte/icons/x';
	import Music from '@lucide/svelte/icons/music';
	import { player, toQueueItems } from '$lib/player/player.svelte';
	import { hueFor, fmtTime, fmtDate } from '$lib/theme/color';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';

	let { data } = $props();

	const items = $derived(data.tracks.items);
	const total = $derived(Number(data.tracks.totalItemCount));

	function togglePlay(index: number) {
		player.playOrToggle(toQueueItems(items), index);
	}
</script>

<svelte:head>
	<title>Tus canciones subidas · Musify</title>
	<meta name="description" content="Todas tus canciones subidas a Musify." />
</svelte:head>

<Page>
	<PageHeader
		title="Tus canciones subidas"
		subtitle="{total} {total === 1 ? 'canción' : 'canciones'} subida{total === 1 ? '' : 's'}."
	>
		{#snippet actions()}
			{#if items.length > 0}
				<Button href="/upload">
					<Upload class="h-4 w-4" />
					Subir canción
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<TrackTable columns="32px 1fr 100px 100px 64px 36px" class="mt-8">
			{#snippet headers()}
				<span class="text-center">#</span>
				<span>Título</span>
				<span class="text-center">Subida</span>
				<span class="text-center">Escuchas</span>
				<span class="text-center">Duración</span>
				<span></span>
			{/snippet}
			{#each items as track, i (track.id)}
				<TrackRow>
					<TrackIndexCell
						index={i}
						active={player.current.id === track.id}
						playing={player.playing}
						onToggle={() => togglePlay(i)}
					/>
					<TrackTitleCell
						trackId={track.id}
						title={track.title}
						hue={hueFor(track.id)}
						onClick={() => togglePlay(i)}
					/>
					<TrackMeta>{fmtDate(track.createdAt)}</TrackMeta>
					<TrackMeta>{Number(track.listensCount)}</TrackMeta>
					<TrackMeta>{fmtTime(Number(track.duration))}</TrackMeta>
					<form
						method="POST"
						action="?/deleteTrack"
						use:enhance={() =>
							async ({ update }) =>
								update()}
					>
						<input type="hidden" name="trackId" value={track.id} />
						<IconButton type="submit" label="Borrar canción" size="sm" revealOnHover>
							<X class="h-3.5 w-3.5" />
						</IconButton>
					</form>
				</TrackRow>
			{/each}
		</TrackTable>
	{:else}
		<EmptyState
			icon={Music}
			title="Todavía no tienes canciones"
			description="Sube la primera y aparecerá aquí lista para reproducir."
		>
			{#snippet actions()}
				<Button href="/upload">
					<Upload class="h-4 w-4" />
					Sube la primera
				</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>
