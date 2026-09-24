<script lang="ts">
	import ListEnd from '@lucide/svelte/icons/list-end';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { player } from '$lib/player/player.svelte';
	import { fetchAlbumTracks } from '$lib/data/albums';
	import type { Menu } from '$lib/state/menu.svelte';
	import type { Playlist } from '$lib/types';
	import ContextMenu from './ContextMenu.svelte';

	interface Props {
		menu: Menu<string>;
		playlists: Playlist[];
	}

	let { menu, playlists }: Props = $props();

	async function playNext(albumId: string) {
		menu.close();
		player.playNext(await fetchAlbumTracks(albumId));
	}

	async function addToQueue(albumId: string) {
		menu.close();
		player.appendToQueue(await fetchAlbumTracks(albumId));
	}
</script>

{#if menu.state}
	{@const { target: albumId, x, y, openLeft } = menu.state}
	<ContextMenu
		{x}
		{y}
		{openLeft}
		onClose={() => menu.close()}
		items={[
			{ icon: ListPlus, label: 'Reproducir a continuación', onclick: () => playNext(albumId) },
			{ icon: ListEnd, label: 'Añadir a la cola', onclick: () => addToQueue(albumId) }
		]}
		playlistAction={{
			action: '?/addAlbumToPlaylist',
			fields: { albumId },
			label: 'Añadir álbum a una playlist'
		}}
		{playlists}
	/>
{/if}
