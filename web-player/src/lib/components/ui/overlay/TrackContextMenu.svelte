<script lang="ts">
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { player } from '$lib/player/player.svelte';
	import type { Menu } from '$lib/state/menu.svelte';
	import type { Playlist, Track } from '$lib/types';
	import ContextMenu from './ContextMenu.svelte';

	interface Props {
		menu: Menu<Track>;
		playlists: Playlist[];
	}

	let { menu, playlists }: Props = $props();

	function playNext(track: Track) {
		player.playNext([track]);
		menu.close();
	}
</script>

{#if menu.state}
	{@const { target: track, x, y, openLeft } = menu.state}
	<ContextMenu
		{x}
		{y}
		{openLeft}
		onClose={() => menu.close()}
		items={[{ icon: ListPlus, label: 'Reproducir a continuación', onclick: () => playNext(track) }]}
		playlistAction={{
			action: '?/addTrack',
			fields: { trackId: track.id },
			label: 'Añadir a una playlist'
		}}
		{playlists}
	/>
{/if}
