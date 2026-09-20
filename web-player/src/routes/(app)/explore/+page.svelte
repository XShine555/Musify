<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player } from '$lib/player/player.svelte';
	import { fetchAlbumQueueItems } from '$lib/albums';
	import { fmtTime, fmtPlays } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/InfiniteScroll.svelte';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import Avatar from '$lib/components/ui/Avatar.svelte';
	import PlayButton from '$lib/components/ui/PlayButton.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import SearchResultRow from '$lib/components/ui/SearchResultRow.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import { EXPLORE_PAGE_SIZE } from '$lib/config';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import AlbumContextMenu, {
		albumContextMenuStateFor,
		type AlbumMenuState
	} from '$lib/components/AlbumContextMenu.svelte';
	import { appendUnique } from '$lib/collections';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetExplicit,
		targetId,
		targetListensCount,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';

	const TOP_RESULT_KIND_LABEL: Record<TopResult['kind'], string> = {
		track: 'Canción',
		album: 'Álbum',
		playlist: 'Playlist',
		user: 'Usuario'
	};

	// No hay sistema de tags/géneros todavía: esta lista es un placeholder de
	// cliente y cada tile enlaza a una búsqueda normal por ese término.
	const GENRE_INFO: Record<string, { label: string; tagline: string }> = {
		pop: { label: 'Pop', tagline: 'Éxitos que suenan en todas partes' },
		rock: { label: 'Rock', tagline: 'Guitarras, actitud y ruido' },
		reggaeton: { label: 'Reggaetón', tagline: 'El ritmo que no para' },
		'lo-fi': { label: 'Lo-fi', tagline: 'Para concentrarte o relajarte' },
		indie: { label: 'Indie', tagline: 'Voces fuera del radar' },
		salsa: { label: 'Salsa', tagline: 'Para mover el cuerpo' },
		'k-pop': { label: 'K-pop', tagline: 'Coreografías pegajosas' },
		electrónica: { label: 'Electrónica', tagline: 'Beats para perderte' },
		jazz: { label: 'Jazz', tagline: 'Improvisación y elegancia' },
		baladas: { label: 'Baladas', tagline: 'Para sentir con calma' },
		trap: { label: 'Trap', tagline: 'Autotune y bajos pesados' },
		cumbia: { label: 'Cumbia', tagline: 'El sabor de siempre' },
		'hip hop': { label: 'Hip hop', tagline: 'Rimas con actitud' },
		'música clásica': { label: 'Clásica', tagline: 'Siglos de composición' }
	};

	const genreTiles = Object.entries(GENRE_INFO).map(([query, info], i, all) => ({
		query,
		hue: Math.round((i * 360) / all.length),
		...info
	}));

	let { data, form } = $props();

	const SEARCH_FILTERS = ['Todo', 'Canciones', 'Álbumes', 'Playlists', 'Usuarios'] as const;
	type SearchFilter = (typeof SEARCH_FILTERS)[number];
	const SEARCH_GROUP_PREVIEW = 4;

	let contextMenu = $state<ContextMenuState | null>(null);
	let albumMenu = $state<AlbumMenuState | null>(null);
	let sfilter = $state<SearchFilter>('Todo');

	type LocalTrack = (typeof data.tracks.items)[number]['track'];

	let localItems = $state<LocalTrack[]>([]);
	let localPage = $state(1);
	let hasMore = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		contextMenu = null;
		albumMenu = null;
		sfilter = 'Todo';
		localItems = data.tracks.items.map((item) => item.track);
		localPage = Number(data.tracks.pageNumber);
		hasMore = data.tracks.hasNextPage;
	});

	const songRows = $derived(
		localItems.map((track) => ({
			target: { track } satisfies TrackTarget,
			seconds: Number(track.duration)
		}))
	);

	const items = $derived<TrackTarget[]>(songRows.map((row) => row.target));

	const albums = $derived(data.albums);
	const users = $derived(data.users);

	const hasAlbums = $derived(albums.length > 0);
	const hasUsers = $derived(users.length > 0);

	const playlistMatches = $derived(
		data.query
			? data.playlists.filter((p) => p.name.toLowerCase().includes(data.query.toLowerCase()))
			: []
	);
	const hasPlaylists = $derived(playlistMatches.length > 0);

	const nothingFound = $derived(
		!!data.query && items.length === 0 && !hasAlbums && !hasUsers && !hasPlaylists
	);

	const searchCounts = $derived({
		Canciones: songRows.length,
		Álbumes: albums.length,
		Playlists: playlistMatches.length,
		Usuarios: users.length
	});
	const totalHits = $derived(
		searchCounts.Canciones + searchCounts.Álbumes + searchCounts.Playlists + searchCounts.Usuarios
	);
	const searchChips = $derived(
		SEARCH_FILTERS.map((label) => ({
			label,
			count: label === 'Todo' ? totalHits : searchCounts[label]
		}))
	);

	type TopResult =
		| { kind: 'track'; target: TrackTarget }
		| { kind: 'album'; album: (typeof albums)[number] }
		| { kind: 'playlist'; playlist: (typeof playlistMatches)[number] }
		| { kind: 'user'; user: (typeof users)[number] };

	const topResult = $derived.by((): TopResult | null => {
		if (sfilter !== 'Todo' || !data.query) return null;
		const q = data.query.trim().toLowerCase();
		const startsWithQuery = (value: string) => value.toLowerCase().startsWith(q);

		const userHit = users.find((u) => startsWithQuery(u.name));
		if (userHit) return { kind: 'user', user: userHit };
		const trackHit = items.find((target) => startsWithQuery(targetTitle(target)));
		if (trackHit) return { kind: 'track', target: trackHit };
		const albumHit = albums.find((album) => startsWithQuery(album.title));
		if (albumHit) return { kind: 'album', album: albumHit };
		const playlistHit = playlistMatches.find((p) => startsWithQuery(p.name));
		if (playlistHit) return { kind: 'playlist', playlist: playlistHit };

		if (users.length) return { kind: 'user', user: users[0] };
		if (items.length) return { kind: 'track', target: items[0] };
		if (albums.length) return { kind: 'album', album: albums[0] };
		if (playlistMatches.length) return { kind: 'playlist', playlist: playlistMatches[0] };
		return null;
	});

	function showGroup(label: Exclude<SearchFilter, 'Todo'>) {
		return sfilter === 'Todo' || sfilter === label;
	}

	function capped<T>(list: T[]) {
		return sfilter === 'Todo' ? list.slice(0, SEARCH_GROUP_PREVIEW) : list;
	}

	function buildHref(query: string) {
		return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
	}

	function playTopResult() {
		if (topResult?.kind !== 'track') return;
		player.playOrToggle([queueItemForTarget(topResult.target)], 0);
	}

	const topResultHref = $derived.by(() => {
		if (!topResult || topResult.kind === 'track') return undefined;
		if (topResult.kind === 'album') return `/albums/${topResult.album.id}`;
		if (topResult.kind === 'playlist') return `/playlists/${topResult.playlist.id}`;
		return `/u/${topResult.user.id}`;
	});

	function togglePlay(index: number) {
		player.playOrToggle(items.map(queueItemForTarget), index);
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}

	function openAlbumMenu(event: MouseEvent, albumId: string) {
		albumMenu = albumContextMenuStateFor(event, albumId);
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

	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemForTarget(contextMenu));
		contextMenu = null;
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
			const res = await fetch(`/api/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			const next = (await res.json()) as typeof data.tracks;
			localItems = appendUnique(
				localItems,
				next.items.map((item) => item.track),
				(track) => track.id
			);
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
	<title>{data.query ? `Buscar «${data.query}»` : 'Descubrir'}</title>
	<meta
		name="description"
		content="Busca canciones, álbumes y personas, o descubre por género en Musify."
	/>
</svelte:head>

<Page>
	{#if !data.query}
		<PageHeader
			title="Descubrir"
			subtitle="Encuentra tu próxima canción favorita explorando por género."
		/>
	{:else if !nothingFound}
		<div>
			<div class="mb-2.5 text-sm font-semibold tracking-[0.16em] text-fg-3 uppercase">
				Resultados de búsqueda
			</div>
			<h1
				class="font-display text-2xl font-semibold tracking-[-0.03em] text-pretty text-fg sm:text-3xl"
			>
				Resultados para «{data.query}»
			</h1>
			<p class="mt-1.75 text-sm text-fg-2">
				{totalHits}
				{totalHits === 1 ? 'Coincidencia' : 'Coincidencias'} en canciones, álbumes, playlists y usuarios
			</p>
		</div>

		<div class="mt-5 flex flex-wrap gap-1.75">
			{#each searchChips as chip (chip.label)}
				<button
					type="button"
					onclick={() => (sfilter = chip.label)}
					class="flex items-center gap-1.75 rounded-full px-3.5 py-1.75 text-xs font-medium transition {sfilter ===
					chip.label
						? 'bg-cta-strong text-ink'
						: 'bg-surface-2 text-fg-2 hover:bg-surface-hover hover:text-fg'}"
				>
					<span>{chip.label}</span>
					<span class="text-xs tabular-nums {sfilter === chip.label ? 'opacity-55' : 'text-muted'}"
						>{chip.count}</span
					>
				</button>
			{/each}
		</div>

		{#if topResult}
			{#snippet topResultBody()}
				{#if topResult.kind === 'track'}
					<Artwork
						trackIds={[targetId(topResult.target)]}
						size="xl"
						alt={targetTitle(topResult.target)}
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
					<div class="text-xs leading-none font-semibold tracking-[0.14em] text-fg-3 uppercase">
						Mejor resultado · {TOP_RESULT_KIND_LABEL[topResult.kind]}
					</div>
					<div
						class="truncate font-display text-lg leading-none font-medium tracking-[-0.02em] text-fg"
					>
						{topResult.kind === 'track'
							? targetTitle(topResult.target)
							: topResult.kind === 'album'
								? topResult.album.title
								: topResult.kind === 'playlist'
									? topResult.playlist.name
									: topResult.user.name}
					</div>
					{#if topResult.kind === 'track' && targetArtist(topResult.target)}
						<div class="truncate text-sm leading-none text-fg-3">
							{targetArtist(topResult.target)}
						</div>
					{/if}
				</div>

				<PlayButton as="span" size="lg" label="Reproducir" class="group-hover/top:brightness-110" />
			{/snippet}

			{#if topResult.kind === 'track'}
				<button
					type="button"
					onclick={playTopResult}
					class="group/top animate-pop mt-6.5 flex w-full items-center gap-5 rounded-panel-lg bg-surface p-4.5 text-left transition hover:bg-surface-hover"
				>
					{@render topResultBody()}
				</button>
			{:else}
				<a
					href={topResultHref}
					class="group/top animate-pop mt-6.5 flex w-full items-center gap-5 rounded-panel-lg bg-surface p-4.5 text-left transition hover:bg-surface-hover"
				>
					{@render topResultBody()}
				</a>
			{/if}
		{/if}
	{/if}

	{#if form?.message}
		<Alert class="mt-4">{form.message}</Alert>
	{/if}

	{#if nothingFound}
		<div class="mx-auto max-w-115 py-[8vh] text-center">
			<h1 class="font-display text-xl font-semibold tracking-tight text-fg">
				Sin resultados para «{data.query}»
			</h1>
			<p class="mt-2.5 text-sm leading-[1.65] text-fg-2">
				Revisa la ortografía o prueba con un término más corto. También puedes buscar por álbum o
				por playlist.
			</p>
			<div class="mt-5.5 flex flex-wrap justify-center gap-2">
				{#each genreTiles.slice(0, 4) as genre (genre.query)}
					<a
						href={buildHref(genre.query)}
						class="rounded-full border border-line-strong px-3.5 py-2 text-xs text-fg-2 transition-colors hover:bg-hover hover:text-fg"
					>
						{genre.label}
					</a>
				{/each}
			</div>
		</div>
	{/if}

	{#if !data.query}
		<!-- GÉNEROS -->
		<div class="mt-7 grid grid-cols-[repeat(auto-fill,minmax(268px,1fr))] gap-3.5">
			{#each genreTiles as genre, i (genre.query)}
				<a
					href={buildHref(genre.query)}
					class="animate-enter group relative flex h-28 flex-col gap-1 overflow-hidden rounded-2xl p-4.25 transition-[filter] hover:brightness-110"
					style="animation-delay:{Math.min(i, 10) *
						40}ms; background:linear-gradient(140deg, oklch(var(--mf-tile-l) var(--mf-tile-c) {genre.hue}), oklch(var(--mf-tile-shade-l) var(--mf-tile-shade-c) {genre.hue}))"
				>
					<div class="text-on-art">
						{genre.label}
					</div>
					<div class="text-xs text-on-art-2">{genre.tagline}</div>
				</a>
			{/each}
		</div>
	{/if}

	{#if data.query && !nothingFound}
		<div class="mt-9 flex flex-col gap-9">
			{#if showGroup('Canciones')}
				<div>
					{#if songRows.length > 0}
						<SectionHeading title="Canciones">
							{#snippet actions()}
								<div class="h-px flex-1 self-center bg-line"></div>
								<span class="shrink-0 text-xs text-muted tabular-nums">
									{searchCounts.Canciones}
									{searchCounts.Canciones === 1 ? 'Resultado' : 'Resultados'}
								</span>
							{/snippet}
						</SectionHeading>
						<ul class="flex flex-col gap-1">
							{#each capped(songRows) as { target: item, seconds }, i (targetId(item))}
								<li>
									<SearchResultRow
										title={targetTitle(item)}
										subtitle={targetArtist(item)}
										meta="{fmtTime(seconds)} · {fmtPlays(targetListensCount(item))}"
										explicit={targetExplicit(item)}
										onclick={() => togglePlay(i)}
										oncontextmenu={(e) => openContextMenu(e, item)}
									>
										{#snippet art(artClass)}
											<Artwork
												trackIds={[targetId(item)]}
												size="lg"
												alt={targetTitle(item)}
												class="{artClass} ring-1 ring-line ring-inset"
											>
												{#if isTargetCurrent(item)}
													<NowPlaying paused={!player.playing} />
												{/if}
											</Artwork>
										{/snippet}
									</SearchResultRow>
								</li>
							{/each}
						</ul>

						{#if sfilter === 'Canciones' && hasMore}
							<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
						{/if}
					{:else}
						<EmptyState
							icon={Music}
							description="No hay canciones que coincidan con «{data.query}»."
						/>
					{/if}
				</div>
			{/if}

			{#if hasAlbums && showGroup('Álbumes')}
				<div>
					<SectionHeading title="Álbumes">
						{#snippet actions()}
							<div class="h-px flex-1 self-center bg-line"></div>
							<span class="shrink-0 text-xs text-muted tabular-nums">
								{searchCounts.Álbumes}
								{searchCounts.Álbumes === 1 ? 'Resultado' : 'Resultados'}
							</span>
						{/snippet}
					</SectionHeading>
					<ul class="flex flex-col gap-1">
						{#each capped(albums) as album (album.id)}
							<li>
								<SearchResultRow
									title={album.title}
									meta="{Number(album.trackCount)} canciones"
									href="/albums/{album.id}"
									oncontextmenu={(e) => openAlbumMenu(e, album.id)}
								>
									{#snippet art(artClass)}
										<Artwork trackIds={album.coverTrackIds} size="lg" class={artClass} />
									{/snippet}
								</SearchResultRow>
							</li>
						{/each}
					</ul>
				</div>
			{/if}

			{#if hasUsers && showGroup('Usuarios')}
				<div>
					<SectionHeading title="Usuarios">
						{#snippet actions()}
							<div class="h-px flex-1 self-center bg-line"></div>
							<span class="shrink-0 text-xs text-muted tabular-nums">
								{searchCounts.Usuarios}
								{searchCounts.Usuarios === 1 ? 'Resultado' : 'Resultados'}
							</span>
						{/snippet}
					</SectionHeading>
					<ul class="flex flex-col gap-1">
						{#each capped(users) as u (u.id)}
							<li>
								<SearchResultRow title={u.name} href="/u/{u.id}">
									{#snippet art(artClass)}
										<Avatar name={u.name} src={u.profilePictureUrl} size="md" class={artClass} />
									{/snippet}
								</SearchResultRow>
							</li>
						{/each}
					</ul>
				</div>
			{/if}

			{#if hasPlaylists && showGroup('Playlists')}
				<div>
					<SectionHeading title="Playlists">
						{#snippet actions()}
							<div class="h-px flex-1 self-center bg-line"></div>
							<span class="shrink-0 text-xs text-muted tabular-nums">
								{searchCounts.Playlists}
								{searchCounts.Playlists === 1 ? 'Resultado' : 'Resultados'}
							</span>
						{/snippet}
					</SectionHeading>
					<ul class="flex flex-col gap-1">
						{#each capped(playlistMatches) as playlist (playlist.id)}
							<li>
								<SearchResultRow
									title={playlist.name}
									subtitle={playlist.description}
									href="/playlists/{playlist.id}"
								>
									{#snippet art(artClass)}
										<Artwork
											src="/api/playlists/{playlist.id}/cover?size=medium&v={encodeURIComponent(
												playlist.updatedAt
											)}"
											trackIds={playlist.coverTrackIds}
											size="lg"
											class={artClass}
										/>
									{/snippet}
								</SearchResultRow>
							</li>
						{/each}
					</ul>
				</div>
			{/if}
		</div>
	{/if}
</Page>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		playlists={data.playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={addToQueue}
	/>
{/if}

{#if albumMenu}
	<AlbumContextMenu
		menu={albumMenu}
		playlists={data.playlists}
		onClose={() => (albumMenu = null)}
		onPlayNext={albumPlayNext}
		onAddToQueue={albumAddToQueue}
	/>
{/if}
