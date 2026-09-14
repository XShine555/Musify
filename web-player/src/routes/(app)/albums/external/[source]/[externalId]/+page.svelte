<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import { player, type QueueItem } from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import AlbumContextMenu, {
		albumContextMenuStateFor,
		type AlbumMenuState
	} from '$lib/components/AlbumContextMenu.svelte';
	import type { TrackTarget } from '$lib/tracks';

	let { data, form } = $props();

	const album = $derived(data.album.album);
	const tracks = $derived(data.album.tracks);

	let contextMenu = $state<ContextMenuState | null>(null);
	let albumMenu = $state<AlbumMenuState | null>(null);

	const queue = $derived<QueueItem[]>(
		tracks.map((track) => ({
			id: track.videoId,
			title: track.title,
			artist: album.artist,
			source: 'youtube' as const,
			coverUrl: album.thumbnailUrl,
			explicit: track.isExplicit
		}))
	);

	const isCurrentQueue = $derived(tracks.some((track) => track.videoId === player.current.id));

	function targetFor(index: number): TrackTarget {
		const track = tracks[index];
		return {
			kind: 'youtube',
			song: {
				videoId: track.videoId,
				title: track.title,
				artist: album.artist,
				album: album.title,
				durationSeconds: Number(track.durationSeconds),
				thumbnailUrl: album.thumbnailUrl,
				isExplicit: track.isExplicit
			}
		};
	}

	function playFrom(index: number) {
		player.playOrToggle(queue, index);
	}

	function playAll() {
		if (queue.length === 0) return;
		if (isCurrentQueue) player.toggle();
		else player.playQueue(queue, 0);
	}
</script>

<svelte:head>
	<title>{album.title}</title>
	<meta name="description" content="Álbum {album.title} de {album.artist}," />
</svelte:head>

<Page>
	<BackLink href="/explore" label="Volver a explorar" />

	<div class="mt-3 flex flex-col gap-5 sm:mt-4 sm:flex-row sm:items-end sm:gap-6">
		<Cover
			trackId={album.albumId}
			src={album.thumbnailUrl}
			size="large"
			alt={album.title}
			class="h-36 w-36 shrink-0 rounded-[18px] shadow-[0_24px_56px_rgba(0,0,0,.55)] sm:h-41 sm:w-41"
			oncontextmenu={(e) => (albumMenu = albumContextMenuStateFor(e, 'youtube', album.albumId))}
		/>
		<div class="min-w-0 flex-1">
			<p class="text-[10.5px] font-semibold tracking-[0.16em] text-fg-2 uppercase">
				{album.isSingle ? 'Single' : album.isEp ? 'EP' : 'Álbum'}
			</p>
			<h1
				class="mt-1.5 font-display text-[28px] font-semibold tracking-[-0.035em] break-words text-fg sm:text-[33px]"
			>
				{album.title}
			</h1>
			<p class="mt-3 text-base text-fg-2">{album.artist}</p>
			<p class="mt-1 text-sm text-muted">
				{[
					album.releaseYear ? String(album.releaseYear) : undefined,
					`${tracks.length} ${tracks.length === 1 ? 'canción' : 'canciones'}`,
					fmtTime(Number(data.album.totalDurationSeconds))
				]
					.filter((part) => !!part)
					.join(' · ')}
			</p>
		</div>
	</div>

	<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
		<Button size="sm" onclick={playAll} disabled={queue.length === 0}>
			{#if isCurrentQueue && player.playing}
				<Pause class="h-4 w-4" strokeWidth={1.5} />
				Pausar
			{:else}
				<Play class="h-4 w-4" strokeWidth={1.5} />
				Reproducir
			{/if}
		</Button>
	</div>

	{#if form?.message}
		<Alert tone="danger" class="mt-4">{form.message}</Alert>
	{/if}

	{#if data.album.description}
		<p class="mt-6 text-sm text-fg-3">{data.album.description}</p>
	{/if}

	<TrackTable index class="mt-6 sm:mt-8">
		{#each tracks as track, i (track.videoId)}
			{@const active = player.current.id === track.videoId}
			<TrackRow
				{active}
				oncontextmenu={(e) => (contextMenu = contextMenuStateFor(e, targetFor(i)))}
			>
				<TrackIndexCell index={i} {active} playing={player.playing} onToggle={() => playFrom(i)} />
				<TrackTitleCell
					trackId={track.videoId}
					title={track.title}
					artist={album.artist}
					coverSrc={album.thumbnailUrl}
					explicit={track.isExplicit}
					{active}
					onClick={() => playFrom(i)}
				/>
				<TrackMeta>{fmtTime(Number(track.durationSeconds))}</TrackMeta>
			</TrackRow>
		{/each}
	</TrackTable>
</Page>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		playlists={data.playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={() => {
			const menu = contextMenu;
			if (menu?.kind === 'youtube') {
				const index = tracks.findIndex((track) => track.videoId === menu.song.videoId);
				if (index >= 0) player.addToQueue(queue[index]);
			}
			contextMenu = null;
		}}
	/>
{/if}

{#if albumMenu}
	<AlbumContextMenu
		menu={albumMenu}
		playlists={data.playlists}
		onClose={() => (albumMenu = null)}
		onPlayNext={() => {
			player.playNext(queue);
			albumMenu = null;
		}}
		onAddToQueue={() => {
			player.appendToQueue(queue);
			albumMenu = null;
		}}
	/>
{/if}
