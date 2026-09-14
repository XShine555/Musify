<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player } from '$lib/player/player.svelte';
	import { fetchAlbumQueueItems } from '$lib/albums';
	import { fmtTime } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/InfiniteScroll.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import AlbumCard from '$lib/components/ui/AlbumCard.svelte';
	import ArtistAvatar from '$lib/components/ui/ArtistAvatar.svelte';
	import Rail from '$lib/components/ui/Rail.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import { EXPLORE_ALBUMS_LIMIT, EXPLORE_PAGE_SIZE } from '$lib/config';
	import type { YouTubeSong } from '$lib/types';
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
		targetCoverSrc,
		targetExplicit,
		targetId,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';

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

	let { data, form } = $props();

	let ytItems = $state<YouTubeSong[]>([]);
	let ytUnavailable = $state(false);
	let contextMenu = $state<ContextMenuState | null>(null);
	let albumMenu = $state<AlbumMenuState | null>(null);

	type LocalTrack = NonNullable<(typeof data.tracks.items)[number]['track']>;

	let localItems = $state<LocalTrack[]>([]);
	let localPage = $state(1);
	let ytContinuation = $state('');
	let hasMore = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		contextMenu = null;
		albumMenu = null;
		localItems = data.tracks.items.flatMap((item) => (item.track ? [item.track] : []));
		localPage = Number(data.tracks.pageNumber);
		ytContinuation = data.tracks.nextYoutubeContinuationToken ?? '';
		hasMore = data.tracks.hasNextPage;

		if (data.query) {
			ytItems = appendUnique(
				[],
				data.tracks.items.flatMap((item) =>
					item.youTubeSong ? [item.youTubeSong as YouTubeSong] : []
				),
				(i) => i.videoId
			);
			ytUnavailable = data.tracks.youtubeUnavailable;
		} else {
			ytItems = [];
			ytUnavailable = false;
		}
	});

	const genreTiles = $derived(
		data.genres.map((query, i) => ({
			query,
			hue: Math.round((i * 360) / data.genres.length),
			...(GENRE_INFO[query] ?? { label: query, tagline: 'Explora este estilo' })
		}))
	);

	const songRows = $derived([
		...localItems.map((track) => ({
			target: { kind: 'local' as const, track },
			seconds: Number(track.duration)
		})),
		...ytItems.map((song) => ({
			target: { kind: 'youtube' as const, song },
			seconds: Number(song.durationSeconds)
		}))
	] satisfies { target: TrackTarget; seconds: number }[]);

	const items = $derived<TrackTarget[]>(songRows.map((row) => row.target));

	const albums = $derived(data.albums);
	const youtubeAlbums = $derived(data.youtubeAlbums);
	const users = $derived(data.users);

	type LocalAlbum = (typeof data.albums)[number];
	type YouTubeAlbum = (typeof data.youtubeAlbums)[number];
	type AlbumEntry = { kind: 'local'; album: LocalAlbum } | { kind: 'youtube'; album: YouTubeAlbum };

	const albumEntries = $derived<AlbumEntry[]>(
		[
			...albums.map((album) => ({ kind: 'local' as const, album })),
			...youtubeAlbums.map((album) => ({ kind: 'youtube' as const, album }))
		].slice(0, EXPLORE_ALBUMS_LIMIT)
	);

	const hasAlbums = $derived(albumEntries.length > 0);
	const hasUsers = $derived(users.length > 0);

	const nothingFound = $derived(
		!!data.query && !ytUnavailable && items.length === 0 && !hasAlbums && !hasUsers
	);

	function buildHref(query: string) {
		return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
	}

	function togglePlay(index: number) {
		player.playOrToggle(items.map(queueItemForTarget), index);
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}

	function openAlbumMenu(event: MouseEvent, kind: 'local' | 'youtube', albumId: string) {
		albumMenu = albumContextMenuStateFor(event, kind, albumId);
	}

	async function albumPlayNext() {
		if (!albumMenu) return;
		const { kind, albumId } = albumMenu;
		albumMenu = null;
		player.playNext(await fetchAlbumQueueItems(kind, albumId));
	}

	async function albumAddToQueue() {
		if (!albumMenu) return;
		const { kind, albumId } = albumMenu;
		albumMenu = null;
		player.appendToQueue(await fetchAlbumQueueItems(kind, albumId));
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
			if (data.query) {
				params.set('name', data.query);
				if (ytContinuation) params.set('youtubeContinuationToken', ytContinuation);
			}
			const res = await fetch(`/api/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			const next = (await res.json()) as typeof data.tracks;
			localItems = appendUnique(
				localItems,
				next.items.flatMap((item) => (item.track ? [item.track] : [])),
				(track) => track.id
			);
			ytItems = appendUnique(
				ytItems,
				next.items.flatMap((item) => (item.youTubeSong ? [item.youTubeSong as YouTubeSong] : [])),
				(song) => song.videoId
			);
			localPage = Number(next.pageNumber);
			ytContinuation = next.nextYoutubeContinuationToken ?? '';
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
		<PageHeader title="Resultados para «{data.query}»" />
	{/if}

	{#if form?.message}
		<Alert class="mt-4">{form.message}</Alert>
	{/if}

	{#if nothingFound}
		<div class="mx-auto max-w-115 py-[8vh] text-center">
			<h1 class="font-display text-xl font-semibold tracking-[-0.025em] text-fg">
				Sin resultados para «{data.query}»
			</h1>
			<p class="mt-2.5 text-[13px] leading-[1.65] text-fg-2">
				Revisa la ortografía o prueba con un término más corto. También puedes buscar por álbum o
				por playlist.
			</p>
			<div class="mt-5.5 flex flex-wrap justify-center gap-2">
				{#each genreTiles.slice(0, 4) as genre (genre.query)}
					<a
						href={buildHref(genre.query)}
						class="rounded-full border border-white/8 px-3.5 py-2 text-xs text-fg-2 transition-colors hover:bg-white/5 hover:text-fg"
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
					class="animate-enter group relative block h-28 overflow-hidden rounded-2xl p-4.25 transition-[filter] hover:brightness-125"
					style="animation-delay:{Math.min(i, 10) *
						40}ms; background:linear-gradient(140deg, oklch(0.24 0.07 {genre.hue}), rgba(9,9,12,.95))"
				>
					<div class="font-display text-[15.5px] font-semibold tracking-[-0.02em] text-fg">
						{genre.label}
					</div>
					<div class="mt-1.5 text-[11.5px] text-fg-3">{genre.tagline}</div>
				</a>
			{/each}
		</div>
	{/if}

	{#if hasAlbums}
		<div class="mt-9">
			<SectionHeading title="Álbumes" />
			<MediaGrid as="ul" min="168px" minMobile="150px">
				{#each albumEntries as entry, i (entry.kind === 'local' ? entry.album.id : entry.album.albumId)}
					<li>
						{#if entry.kind === 'local'}
							<AlbumCard
								id={entry.album.id}
								title={entry.album.title}
								releaseYear={entry.album.releaseYear === null
									? undefined
									: Number(entry.album.releaseYear)}
								trackCount={Number(entry.album.trackCount)}
								trackIds={entry.album.coverTrackIds}
								index={i}
								onContextMenu={(e) => openAlbumMenu(e, 'local', entry.album.id)}
							/>
						{:else}
							<AlbumCard
								id={entry.album.albumId}
								href="/albums/external/youtube/{entry.album.albumId}"
								title={entry.album.title}
								subtitle={entry.album.artist}
								releaseYear={entry.album.releaseYear === null
									? undefined
									: Number(entry.album.releaseYear)}
								coverSrc={entry.album.thumbnailUrl}
								index={i}
								onContextMenu={(e) => openAlbumMenu(e, 'youtube', entry.album.albumId)}
							/>
						{/if}
					</li>
				{/each}
			</MediaGrid>
		</div>
	{/if}

	{#if hasUsers}
		<div class="mt-9">
			<SectionHeading title="Usuarios" />
			<Rail>
				{#each users as u, i (u.id)}
					<a
						href="/u/{u.id}"
						class="group/artist animate-enter shrink-0"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<ArtistAvatar name={u.name} imageUrl={u.profilePictureUrl} size={96} />
					</a>
				{/each}
			</Rail>
		</div>
	{/if}

	{#if data.query}
		<div class="mt-9">
			{#if songRows.length > 0}
				<SectionHeading title="Canciones" />
				<ul class="flex flex-col gap-1">
					{#each songRows as { target: item, seconds }, i (targetId(item))}
						<li>
							<button
								type="button"
								onclick={() => togglePlay(i)}
								oncontextmenu={(e) => openContextMenu(e, item)}
								aria-label="Reproducir {targetTitle(item)}"
								class="group/row flex w-full min-w-0 items-center gap-3.5 rounded-control p-2 pr-4 text-left transition hover:bg-surface focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none"
							>
								<Cover
									trackId={targetId(item)}
									src={targetCoverSrc(item)}
									size="small"
									alt={targetTitle(item)}
									class="h-11 w-11 shrink-0 rounded-control shadow-art ring-1 ring-line ring-inset"
								>
									{#if isTargetCurrent(item)}
										<NowPlaying paused={!player.playing} />
									{/if}
								</Cover>
								<div class="min-w-0 flex-1">
									<div class="flex min-w-0 items-center gap-1.5">
										{#if targetExplicit(item)}
											<ExplicitBadge />
										{/if}
										<span class="truncate text-[13px] font-medium text-fg">
											{targetTitle(item)}
										</span>
									</div>
									{#if targetArtist(item)}
										<div class="truncate text-[11.5px] text-fg-3">
											Canción · {targetArtist(item)}
										</div>
									{/if}
								</div>
								<span class="shrink-0 text-[11.5px] text-muted tabular-nums"
									>{fmtTime(seconds)}</span
								>
							</button>
						</li>
					{/each}
				</ul>

				{#if hasMore}
					<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
				{/if}
			{:else if !ytUnavailable && !nothingFound}
				<EmptyState icon={Music} description="No hay canciones que coincidan con «{data.query}»." />
			{/if}
		</div>
	{/if}

	{#if ytUnavailable}
		<EmptyState
			icon={Music}
			description="YouTube Music no está disponible ahora mismo. Inténtalo de nuevo."
		/>
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
