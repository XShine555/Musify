<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import Avatar from '$lib/components/ui/media/Avatar.svelte';
	import PlayButton from '$lib/components/ui/media/PlayButton.svelte';
	import { playlistCover } from '$lib/utils/hrefs';
	import type { Album, Playlist, Track } from '$lib/types';
	import type { TopResult as TopResultData } from '$lib/data/search';

	interface ResultUser {
		id: string;
		name: string;
		profilePictureUrl: string | null;
	}

	interface Props {
		result: TopResultData<Track, Album, Playlist, ResultUser>;
	}

	let { result }: Props = $props();

	const KIND_LABEL: Record<Props['result']['kind'], string> = {
		track: 'Canción',
		album: 'Álbum',
		playlist: 'Playlist',
		user: 'Usuario'
	};

	function play() {
		if (result.kind !== 'track') return;
		player.playOrToggle([result.track], 0);
	}

	const href = $derived.by(() => {
		if (result.kind === 'track') return undefined;
		if (result.kind === 'album') return `/albums/${result.album.id}`;
		if (result.kind === 'playlist') return `/playlists/${result.playlist.id}`;
		return `/user/${result.user.id}`;
	});
</script>

{#snippet body()}
	{#if result.kind === 'track'}
		<Artwork trackIds={[result.track.id]} size="xl" alt={result.track.title} class="shrink-0" />
	{:else if result.kind === 'album'}
		<Artwork trackIds={result.album.coverTrackIds} size="xl" class="shrink-0" />
	{:else if result.kind === 'playlist'}
		<Artwork
			src={playlistCover(result.playlist, 'large')}
			trackIds={result.playlist.coverTrackIds}
			size="xl"
			class="shrink-0"
		/>
	{:else}
		<Avatar name={result.user.name} src={result.user.profilePictureUrl} size="lg" />
	{/if}

	<div class="flex h-22 min-w-0 flex-1 flex-col justify-center gap-3">
		<div class="text-eyebrow leading-none font-semibold text-fg-2">
			Mejor resultado · {KIND_LABEL[result.kind]}
		</div>
		<div class="truncate font-display text-lg leading-none font-medium tracking-display text-fg">
			{result.kind === 'track'
				? result.track.title
				: result.kind === 'album'
					? result.album.title
					: result.kind === 'playlist'
						? result.playlist.name
						: result.user.name}
		</div>
		{#if result.kind === 'track' && result.track.artist}
			<div class="truncate text-sm leading-none text-fg-2">
				{result.track.artist}
			</div>
		{/if}
	</div>

	<PlayButton as="span" size="lg" label="Reproducir" class="group-hover/top:brightness-110" />
{/snippet}

<svelte:element
	this={result.kind === 'track' ? 'button' : 'a'}
	type={result.kind === 'track' ? 'button' : undefined}
	role={result.kind === 'track' ? 'button' : 'link'}
	{href}
	onclick={result.kind === 'track' ? play : undefined}
	class="group/top animate-pop mt-6.5 flex w-full items-center gap-5 rounded-panel-lg bg-surface p-4.5 text-left transition hover:bg-surface-hover"
>
	{@render body()}
</svelte:element>
