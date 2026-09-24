<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player, toQueueItem } from '$lib/player/player.svelte';
	import { fetchAlbumQueueItems } from '$lib/data/albums';
	import { fmtTime, fmtPlays, plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/primitives/InfiniteScroll.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import Avatar from '$lib/components/ui/media/Avatar.svelte';
	import UserRow from '$lib/components/ui/media/UserRow.svelte';
	import PlayButton from '$lib/components/ui/media/PlayButton.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import Chip from '$lib/components/ui/primitives/Chip.svelte';
	import SectionHeading from '$lib/components/ui/layout/SectionHeading.svelte';
	import { EXPLORE_PAGE_SIZE } from '$lib/config';
	import { genreHref } from '$lib/state/navigation.svelte';
	import { createTrackMenu } from '$lib/state/menus.svelte';
	import ContextMenu, { contextMenuPosition } from '$lib/components/ui/overlay/ContextMenu.svelte';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import ListEnd from '@lucide/svelte/icons/list-end';
	import { appendUnique } from '$lib/data/collections';
	import { genreInfo, genreTiles } from '$lib/data/genres';
	import {
		type SearchFilter,
		showGroup,
		capped,
		matchPlaylists,
		searchCounts,
		searchChips,
		findTopResult
	} from '$lib/data/search';

	type AlbumMenuState = { x: number; y: number; openLeft: boolean; albumId: string };

	const TOP_RESULT_KIND_LABEL: Record<TopResult['kind'], string> = {
		track: 'Canción',
		album: 'Álbum',
		playlist: 'Playlist',
		user: 'Usuario'
	};

	let { data, form } = $props();

	const trackMenu = createTrackMenu();
	let albumMenu = $state<AlbumMenuState | null>(null);
	let sfilter = $state<SearchFilter>('Todo');

	type LocalTrack = (typeof data.tracks.items)[number];

	let localItems = $state<LocalTrack[]>([]);
	let localPage = $state(1);
	let hasMore = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		trackMenu.close();
		albumMenu = null;
		sfilter = data.genre ? 'Canciones' : 'Todo';
		localItems = data.tracks.items;
		localPage = Number(data.tracks.pageNumber);
		hasMore = data.tracks.hasNextPage;
	});

	const songRows = $derived(
		localItems.map((track) => ({
			track,
			seconds: Number(track.duration)
		}))
	);

	const items = $derived(songRows.map((row) => row.track));

	const tiles = $derived(genreTiles(data.genres));
	const activeGenre = $derived(data.genre ? genreInfo(data.genre) : null);

	const albums = $derived(data.albums);
	const users = $derived(data.users);

	const hasAlbums = $derived(albums.length > 0);
	const hasUsers = $derived(users.length > 0);

	const playlistMatches = $derived(matchPlaylists(data.playlists, data.query));
	const hasPlaylists = $derived(playlistMatches.length > 0);

	const nothingFound = $derived(
		!!data.query && items.length === 0 && !hasAlbums && !hasUsers && !hasPlaylists
	);

	const counts = $derived(
		searchCounts({
			tracks: songRows.length,
			albums: albums.length,
			playlists: playlistMatches.length,
			users: users.length
		})
	);
	const chips = $derived(searchChips(counts));
	const totalHits = $derived(
		counts.Canciones + counts.Álbumes + counts.Playlists + counts.Usuarios
	);

	type TopResult = NonNullable<
		ReturnType<
			typeof findTopResult<
				LocalTrack,
				(typeof albums)[number],
				(typeof playlistMatches)[number],
				(typeof users)[number]
			>
		>
	>;

	const topResult = $derived<TopResult | null>(
		sfilter === 'Todo' && data.query
			? findTopResult({
					query: data.query,
					tracks: items,
					albums,
					playlists: playlistMatches,
					users
				})
			: null
	);

	function playTopResult() {
		if (topResult?.kind !== 'track') return;
		player.playOrToggle([toQueueItem(topResult.track)], 0);
	}

	const topResultHref = $derived.by(() => {
		if (!topResult || topResult.kind === 'track') return undefined;
		if (topResult.kind === 'album') return `/albums/${topResult.album.id}`;
		if (topResult.kind === 'playlist') return `/playlists/${topResult.playlist.id}`;
		return `/user/${topResult.user.id}`;
	});

	function togglePlay(index: number) {
		player.playOrToggle(items.map(toQueueItem), index);
	}

	function openContextMenu(event: MouseEvent, track: LocalTrack) {
		trackMenu.open(event, track);
	}

	function openAlbumMenu(event: MouseEvent, albumId: string) {
		albumMenu = { ...contextMenuPosition(event), albumId };
	}

	async function albumPlayNext() {
		if (!albumMenu) return;
		const { albumId } = albumMenu;
		albumMenu = null;
		player.playNext(await fetchAlbumQueueItems(albumId));
	}

	async function albumAddToQueue() {
		if (!albumMenu) return;
		const { albumId } = albumMenu;
		albumMenu = null;
		player.appendToQueue(await fetchAlbumQueueItems(albumId));
	}

	async function loadMore() {
		if (loadingMore) return;
		loadingMore = true;
		try {
			const params = new URLSearchParams({
				pageNumber: String(localPage + 1),
				pageSize: String(EXPLORE_PAGE_SIZE)
			});
			if (data.query) params.set('name', data.query);
			if (data.genre) params.set('genre', data.genre);
			const res = await fetch(`/api/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			const next = (await res.json()) as typeof data.tracks;
			localItems = appendUnique(localItems, next.items, (track) => track.id);
			localPage = Number(next.pageNumber);
			hasMore = next.hasNextPage;
		} catch {
			hasMore = false;
		} finally {
			loadingMore = false;
		}
	}
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
			{#each chips as chip (chip.label)}
				<Chip
					selected={sfilter === chip.label}
					count={chip.count}
					onclick={() => (sfilter = chip.label)}
				>
					{chip.label}
				</Chip>
			{/each}
		</div>

		{#if topResult}
			{#snippet topResultBody()}
				{#if topResult.kind === 'track'}
					<Artwork
						trackIds={[topResult.track.id]}
						size="xl"
						alt={topResult.track.title}
						class="shrink-0"
					/>
				{:else if topResult.kind === 'album'}
					<Artwork trackIds={topResult.album.coverTrackIds} size="xl" class="shrink-0" />
				{:else if topResult.kind === 'playlist'}
					<Artwork
						src="/api/playlists/{topResult.playlist.id}/cover?size=large&v={encodeURIComponent(
							topResult.playlist.updatedAt
						)}"
						trackIds={topResult.playlist.coverTrackIds}
						size="xl"
						class="shrink-0"
					/>
				{:else}
					<Avatar name={topResult.user.name} src={topResult.user.profilePictureUrl} size="lg" />
				{/if}

				<div class="flex h-22 min-w-0 flex-1 flex-col justify-center gap-3">
					<div class="text-eyebrow leading-none font-semibold text-fg-2">
						Mejor resultado · {TOP_RESULT_KIND_LABEL[topResult.kind]}
					</div>
					<div
						class="truncate font-display text-lg leading-none font-medium tracking-display text-fg"
					>
						{topResult.kind === 'track'
							? topResult.track.title
							: topResult.kind === 'album'
								? topResult.album.title
								: topResult.kind === 'playlist'
									? topResult.playlist.name
									: topResult.user.name}
					</div>
					{#if topResult.kind === 'track' && topResult.track.artist}
						<div class="truncate text-sm leading-none text-fg-2">
							{topResult.track.artist}
						</div>
					{/if}
				</div>

				<PlayButton as="span" size="lg" label="Reproducir" class="group-hover/top:brightness-110" />
			{/snippet}

			<svelte:element
				this={topResult.kind === 'track' ? 'button' : 'a'}
				type={topResult.kind === 'track' ? 'button' : undefined}
				role={topResult.kind === 'track' ? 'button' : 'link'}
				href={topResult.kind === 'track' ? undefined : topResultHref}
				onclick={topResult.kind === 'track' ? playTopResult : undefined}
				class="group/top animate-pop mt-6.5 flex w-full items-center gap-5 rounded-panel-lg bg-surface p-4.5 text-left transition hover:bg-surface-hover"
			>
				{@render topResultBody()}
			</svelte:element>
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
		<div class="grid-wide">
			{#each tiles as tile, i (tile.genre)}
				<a
					href={genreHref(tile.genre)}
					class="animate-enter group relative flex h-28 flex-col gap-1 overflow-hidden rounded-panel p-4 transition-[filter] genre-tile hover:brightness-110"
					style="--i:{i}; --tile-hue:{tile.hue}"
				>
					<div class="text-on-art">
						{tile.label}
					</div>
					<div class="text-xs text-on-art-2">{tile.tagline}</div>
				</a>
			{/each}
		</div>
		{#if tiles.length === 0}
			<EmptyState icon={Music} description="Todavía no hay géneros. Sube música para empezar." />
		{/if}
	{/if}

	{#if (data.query || data.genre) && !nothingFound}
		<div class="mt-9 flex flex-col gap-9">
			{#if showGroup(sfilter, 'Canciones')}
				<div>
					{#if songRows.length > 0}
						<SectionHeading title="Canciones" count={counts.Canciones} />
						<ul class="flex flex-col gap-1">
							{#each capped(sfilter, songRows) as { track, seconds }, i (track.id)}
								<li>
									<ListRow
										title={track.title}
										subtitle={track.artist}
										subtitleHref={track.ownerUserId}
										explicit={track.isExplicit}
										active={player.current.id === track.id}
										size="lg"
										trackId={track.id}
										onclick={() => togglePlay(i)}
										oncontextmenu={(e) => openContextMenu(e, track)}
									>
										{#snippet overlay()}
											{#if player.current.id === track.id}
												<EqBars overlay paused={!player.playing} />
											{/if}
										{/snippet}
										{#snippet trailing()}
											<span class="hidden shrink-0 text-xs text-fg-2 tabular-nums sm:block">
												{fmtTime(seconds)} · {fmtPlays(track.listensCount)}
											</span>
										{/snippet}
									</ListRow>
								</li>
							{/each}
						</ul>

						{#if sfilter === 'Canciones' && hasMore}
							<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
						{/if}
					{:else}
						<EmptyState
							icon={Music}
							description={activeGenre
								? 'Todavía no hay canciones de este género.'
								: `No hay canciones que coincidan con «${data.query}».`}
						/>
					{/if}
				</div>
			{/if}

			{#if hasAlbums && showGroup(sfilter, 'Álbumes')}
				<div>
					<SectionHeading title="Álbumes" count={counts.Álbumes} />
					<ul class="flex flex-col gap-1">
						{#each capped(sfilter, albums) as album (album.id)}
							<li>
								<ListRow
									title={album.title}
									href="/albums/{album.id}"
									size="lg"
									trackIds={album.coverTrackIds}
									oncontextmenu={(e) => openAlbumMenu(e, album.id)}
								>
									{#snippet trailing()}
										<span class="hidden shrink-0 text-xs text-fg-2 tabular-nums sm:block">
											{Number(album.trackCount)} canciones
										</span>
									{/snippet}
								</ListRow>
							</li>
						{/each}
					</ul>
				</div>
			{/if}

			{#if hasUsers && showGroup(sfilter, 'Usuarios')}
				<div>
					<SectionHeading title="Usuarios" count={counts.Usuarios} />
					<ul class="flex flex-col gap-1">
						{#each capped(sfilter, users) as u (u.id)}
							<li>
								<UserRow user={u} />
							</li>
						{/each}
					</ul>
				</div>
			{/if}

			{#if hasPlaylists && showGroup(sfilter, 'Playlists')}
				<div>
					<SectionHeading title="Playlists" count={counts.Playlists} />
					<ul class="flex flex-col gap-1">
						{#each capped(sfilter, playlistMatches) as playlist (playlist.id)}
							<li>
								<ListRow
									title={playlist.name}
									subtitle={playlist.description}
									href="/playlists/{playlist.id}"
									size="lg"
									coverSrc="/api/playlists/{playlist.id}/cover?size=medium&v={encodeURIComponent(
										playlist.updatedAt
									)}"
									trackIds={playlist.coverTrackIds}
								/>
							</li>
						{/each}
					</ul>
				</div>
			{/if}
		</div>
	{/if}
</Page>

{#if trackMenu.state}
	<ContextMenu
		x={trackMenu.state.x}
		y={trackMenu.state.y}
		openLeft={trackMenu.state.openLeft}
		onClose={() => trackMenu.close()}
		items={[
			{ icon: ListPlus, label: 'Reproducir a continuación', onclick: () => trackMenu.playNext() }
		]}
		playlistAction={{
			action: '?/addTrack',
			fields: { trackId: String(trackMenu.state.track.id) },
			label: 'Añadir a una playlist'
		}}
		playlists={data.playlists}
	/>
{/if}

{#if albumMenu}
	<ContextMenu
		x={albumMenu.x}
		y={albumMenu.y}
		openLeft={albumMenu.openLeft}
		onClose={() => (albumMenu = null)}
		items={[
			{ icon: ListPlus, label: 'Reproducir a continuación', onclick: albumPlayNext },
			{ icon: ListEnd, label: 'Añadir a la cola', onclick: albumAddToQueue }
		]}
		playlistAction={{
			action: '?/addAlbumToPlaylist',
			fields: { albumId: albumMenu.albumId },
			label: 'Añadir álbum a una playlist'
		}}
		playlists={data.playlists}
	/>
{/if}
