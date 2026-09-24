<script lang="ts">
	import type { Paged, Track } from '$lib/types';
	import { plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Chip from '$lib/components/ui/primitives/Chip.svelte';
	import { EXPLORE_PAGE_SIZE } from '$lib/config';
	import { genreHref } from '$lib/utils/hrefs';
	import { createMenu } from '$lib/state/menu.svelte';
	import { createPagedList } from '$lib/state/pagedList.svelte';
	import TrackContextMenu from '$lib/components/music/TrackContextMenu.svelte';
	import AlbumContextMenu from '$lib/components/music/AlbumContextMenu.svelte';
	import { genreInfo, genreTiles } from '$lib/data/genres';
	import {
		type SearchFilter,
		matchPlaylists,
		searchTotal,
		searchChips,
		findTopResult
	} from '$lib/data/search';
	import GenreGrid from './GenreGrid.svelte';
	import TopResult from './TopResult.svelte';
	import SearchResults from './SearchResults.svelte';

	let { data, form } = $props();

	const trackMenu = createMenu<Track>();
	const albumMenu = createMenu<string>();
	let sfilter = $state<SearchFilter>('all');

	const tracksList = createPagedList<Track>(
		async (pageNumber) => {
			const params = new URLSearchParams({
				pageNumber: String(pageNumber),
				pageSize: String(EXPLORE_PAGE_SIZE)
			});
			if (data.query) params.set('name', data.query);
			if (data.genre) params.set('genre', data.genre);
			const res = await fetch(`/api/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			return (await res.json()) as Paged<Track>;
		},
		(track) => track.id
	);

	$effect(() => {
		trackMenu.close();
		albumMenu.close();
		sfilter = data.genre ? 'tracks' : 'all';
		tracksList.reset(data.tracks);
	});

	const items = $derived(tracksList.items);

	const tiles = $derived(genreTiles(data.genres));
	const activeGenre = $derived(data.genre ? genreInfo(data.genre) : null);

	const albums = $derived(data.albums);
	const users = $derived(data.users);

	const hasAlbums = $derived(albums.length > 0);
	const hasUsers = $derived(users.length > 0);

	const playlistMatches = $derived(matchPlaylists(data.userPlaylists, data.query));
	const hasPlaylists = $derived(playlistMatches.length > 0);

	const nothingFound = $derived(
		!!data.query && items.length === 0 && !hasAlbums && !hasUsers && !hasPlaylists
	);

	const counts = $derived({
		tracks: data.tracks.totalItemCount,
		albums: data.albumsTotal,
		playlists: playlistMatches.length,
		users: data.usersTotal
	});
	const chips = $derived(searchChips(counts));
	const totalHits = $derived(searchTotal(counts));

	const topResult = $derived(
		sfilter === 'all' && data.query
			? findTopResult({
					query: data.query,
					tracks: items,
					albums,
					playlists: playlistMatches,
					users
				})
			: null
	);
</script>

<svelte:head>
	<title
		>{activeGenre ? activeGenre.label : data.query ? `Buscar «${data.query}»` : 'Descubrir'}</title
	>
	<meta
		name="description"
		content="Busca canciones, álbumes y personas, o descubre por género en Musify."
	/>
</svelte:head>

<Page>
	{#if activeGenre}
		<PageHeader eyebrow="Género" title={activeGenre.label} description={activeGenre.tagline} />
	{:else if !data.query}
		<PageHeader
			title="Descubrir"
			description="Encuentra tu próxima canción favorita explorando por género."
		/>
	{:else if !nothingFound}
		<PageHeader
			eyebrow="Resultados de búsqueda"
			title="Resultados para «{data.query}»"
			description="{plural(
				totalHits,
				'coincidencia',
				'coincidencias'
			)} en canciones, álbumes, playlists y usuarios"
		/>

		<div class="mb-7 flex flex-wrap gap-2 sm:mb-8">
			{#each chips as chip (chip.filter)}
				<Chip
					selected={sfilter === chip.filter}
					count={chip.count}
					onclick={() => (sfilter = chip.filter)}
				>
					{chip.label}
				</Chip>
			{/each}
		</div>

		{#if topResult}
			<TopResult result={topResult} />
		{/if}
	{/if}

	{#if form?.message}
		<Alert class="mt-4">{form.message}</Alert>
	{/if}

	{#if nothingFound}
		<div class="mx-auto max-w-prose-sm py-[8vh] text-center">
			<h1 class="text-display-3 text-fg">
				Sin resultados para «{data.query}»
			</h1>
			<p class="mt-2.5 text-body text-fg-2">
				Revisa la ortografía o prueba con un término más corto. También puedes buscar por álbum o
				por playlist.
			</p>
			<div class="mt-5.5 flex flex-wrap justify-center gap-2">
				{#each tiles.slice(0, 4) as tile (tile.genre)}
					<Chip href={genreHref(tile.genre)}>{tile.label}</Chip>
				{/each}
			</div>
		</div>
	{/if}

	{#if !data.query && !data.genre}
		<GenreGrid {tiles} />
	{/if}

	{#if (data.query || data.genre) && !nothingFound}
		<SearchResults
			filter={sfilter}
			{counts}
			query={data.query}
			inGenre={!!activeGenre}
			{tracksList}
			{albums}
			playlists={playlistMatches}
			{users}
			{trackMenu}
			{albumMenu}
		/>
	{/if}
</Page>

<TrackContextMenu menu={trackMenu} playlists={data.userPlaylists} />
<AlbumContextMenu menu={albumMenu} playlists={data.userPlaylists} />
