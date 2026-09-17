<script lang="ts">
	import { goto } from '$app/navigation';
	import History from '@lucide/svelte/icons/history';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import { player } from '$lib/player/player.svelte';
	import { mergeRecentlyPlayed } from '$lib/recentlyPlayed';
	import { shuffle as shuffleList } from '$lib/collections';
	import { fmtTime, fmtDurationLong, fmtPlays } from '$lib/format';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetExplicit,
		targetForQueueItem,
		targetId,
		targetListensCount,
		targetOwnerUserId,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';
	import ArtistLink from '$lib/components/ui/ArtistLink.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import MixTile from '$lib/components/ui/MixTile.svelte';
	import EqBars from '$lib/components/ui/EqBars.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import { HOME_CONTINUE_LIMIT, HOME_POPULAR_MAX } from '$lib/config';

	let { data } = $props();

	let contextMenu = $state<ContextMenuState | null>(null);

	type SpotlightRow = { target: TrackTarget; seconds: number };
	let spotlightRows = $state<SpotlightRow[]>([]);
	let spotlightTotal = $state(0);

	$effect(() => {
		spotlightRows = [];
		spotlightTotal = 0;
		let cancelled = false;
		data.spotlightTracks.then(({ items, totalCount }) => {
			if (cancelled) return;
			spotlightRows = items.map((track) => ({
				target: { track },
				seconds: Number(track.duration)
			}));
			spotlightTotal = totalCount;
		});
		return () => {
			cancelled = true;
		};
	});

	const mixes = $derived(data.mixes);
	const playlists = $derived(data.playlists);
	const spotlight = $derived(data.spotlightPlaylist);
	const spotlightItems = $derived(spotlightRows.map((row) => row.target));
	const spotlightDurationSeconds = $derived(
		spotlightRows.reduce((sum, row) => sum + row.seconds, 0)
	);
	const spotlightTrackCount = $derived(spotlightTotal || spotlightItems.length);
	const spotlightVisibilityLabel = $derived(
		spotlight?.visibility === 'Public' ? 'Pública' : 'Privada'
	);
	const spotlightMeta = $derived(
		spotlightTrackCount === 0
			? spotlightVisibilityLabel
			: [
					`${spotlightTrackCount} ${spotlightTrackCount === 1 ? 'canción' : 'canciones'}`,
					fmtDurationLong(spotlightDurationSeconds),
					spotlightVisibilityLabel
				].join(' · ')
	);

	const continueItems = $derived(mergeRecentlyPlayed(data.recentlyPlayed, HOME_CONTINUE_LIMIT));
	const recentPool = $derived(mergeRecentlyPlayed(data.recentlyPlayed, 24));

	const popularTargets = $derived<TrackTarget[]>(
		recentPool.map(targetForQueueItem).slice(0, HOME_POPULAR_MAX)
	);

	const spotlightQueue = $derived(spotlightItems.map(queueItemForTarget));
	const spotlightIsCurrent = $derived(
		spotlightQueue.length > 0 && spotlightQueue.some((item) => item.id === player.current.id)
	);
	const heroPlaying = $derived(spotlightIsCurrent && player.playing);

	function playContinue(index: number) {
		player.playOrToggle(continueItems, index);
	}
	function playPopular(index: number) {
		player.playOrToggle(popularTargets.map(queueItemForTarget), index);
	}
	function playSpotlightTrack(index: number) {
		player.playOrToggle(spotlightQueue, index);
	}
	function playSpotlight() {
		if (spotlightQueue.length === 0) return;
		if (spotlightIsCurrent) player.toggle();
		else player.playQueue(spotlightQueue, 0);
	}
	function shuffleSpotlight() {
		if (spotlightQueue.length === 0) return;
		player.playQueue(shuffleList(spotlightQueue), 0);
	}

	function onRowKeydown(event: KeyboardEvent, action: () => void) {
		if (event.key !== 'Enter' && event.key !== ' ') return;
		event.preventDefault();
		action();
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}
	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemForTarget(contextMenu));
		contextMenu = null;
	}
</script>

<svelte:head>
	<title>Musify</title>
	<meta name="description" content="Tu música, sin límites." />
</svelte:head>

<section class="page-x pt-3 sm:pt-4">
	<div
		class="relative overflow-hidden rounded-3xl bg-[image:var(--mf-hero-bg)] px-6 py-8 transition-[background] duration-500 sm:px-9 sm:py-8.5"
	>
		<div
			class="relative flex flex-col items-start gap-7 lg:flex-row lg:flex-wrap lg:items-end lg:justify-between"
		>
			<div class="animate-enter max-w-135 min-w-65">
				<h1
					class="font-display text-3xl leading-tight font-medium tracking-tight text-pretty text-fg sm:text-4xl"
				>
					{data.greeting}
				</h1>
				<p class="mt-3.5 max-w-108 text-sm leading-[1.65] text-fg-2">
					Sube lo tuyo, descubre lo nuevo y escúchalo todo en un solo sitio.
				</p>
				<div class="mt-6.5 flex flex-wrap items-center gap-2.5">
					{#if spotlight}
						<Button
							variant="accent"
							class="!bg-cta-strong !text-ink hover:!brightness-95"
							onclick={playSpotlight}
						>
							{heroPlaying ? 'Pausar' : 'Reanudar'}
							{spotlight.name}
						</Button>
					{/if}
					<Button href="/explore" variant="secondary" class="!font-normal">Explorar música</Button>
				</div>
			</div>

			{#await data.listeningStats then stats}
				<div class="flex gap-6.5">
					<div>
						<div class="font-display text-2xl font-medium tracking-tight text-fg">
							{stats.tracksThisWeek}
						</div>
						<div class="mt-1 text-xs text-fg-3">Canciones nuevas escuchadas esta semana</div>
					</div>
					<div>
						<div class="font-display text-2xl font-medium tracking-tight text-fg">
							{fmtDurationLong(Number(stats.secondsThisWeek))}
						</div>
						<div class="mt-1 text-xs text-fg-3">Tiempo de escucha esta semana</div>
					</div>
					<div>
						<div class="font-display text-2xl font-medium tracking-tight text-fg">
							{stats.streakDays}
						</div>
						<div class="mt-1 text-xs text-fg-3">Días consecutivos escuchando</div>
					</div>
				</div>
			{/await}
		</div>
	</div>
</section>

<div class="flex flex-col gap-11.5 page-x pt-10 pb-8 sm:pb-10">
	<!-- CONTINUAR ESCUCHANDO -->
	<section>
		<SectionHeading title="Continuar escuchando" subtitle="Retomalo donde lo dejaste" />
		{#if continueItems.length > 0}
			<div class="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-5">
				{#each continueItems as item, i (item.id)}
					{@const isCurrent = player.current.id === item.id}
					<div
						role="button"
						tabindex="0"
						class="animate-enter rounded-art p-2.25 pr-3.5 transition-colors {isCurrent
							? 'bg-accent-tint'
							: 'hover:bg-hover'}"
						style="animation-delay:{Math.min(i, 10) * 40}ms"
						onclick={() => playContinue(i)}
						onkeydown={(e) => onRowKeydown(e, () => playContinue(i))}
						oncontextmenu={(e) => openContextMenu(e, targetForQueueItem(item))}
					>
						<TrackTitleCell
							trackId={item.id}
							title={item.title}
							artist={item.artist}
							ownerUserId={item.ownerUserId}
							coverSize="h-14 w-14"
							active={isCurrent}
							onClick={() => playContinue(i)}
						/>
					</div>
				{/each}
			</div>
		{:else}
			<EmptyState
				icon={History}
				title="Todavía no has escuchado nada"
				description="Reproduce alguna canción y tu actividad reciente aparecerá aquí."
			>
				{#snippet actions()}
					<Button href="/explore" variant="secondary">Explorar música</Button>
				{/snippet}
			</EmptyState>
		{/if}
	</section>

	<!-- PLAYLIST DESTACADA -->
	{#if spotlight}
		<section>
			<div
				class="grid grid-cols-1 overflow-hidden rounded-panel-lg transition-[background] duration-500 lg:grid-cols-2"
				style="background-image:var(--mf-spotlight-bg), linear-gradient(to right, transparent 50%, color-mix(in oklch, var(--mf-scrim) 22%, transparent) 61%, transparent 92.5%)"
			>
				<div class="flex flex-col justify-center p-8 sm:p-9">
					<div
						role="button"
						tabindex="0"
						class="flex w-fit items-center gap-6 self-start"
						onclick={() => goto(`/playlists/${spotlight.id}`)}
						onkeydown={(e) => onRowKeydown(e, () => goto(`/playlists/${spotlight.id}`))}
					>
						<PlaylistArt
							playlistId={spotlight.id}
							trackIds={spotlight.coverTrackIds}
							version={spotlight.updatedAt}
							size="large"
							class="h-34 w-34 shrink-0 rounded-2xl"
						/>
						<div class="flex min-w-0 flex-col gap-4">
							<p class="text-xs font-medium tracking-widest text-fg-2 uppercase">
								Playlist destacada
							</p>
							<h3 class="truncate font-display text-2xl font-medium text-fg">
								{spotlight.name}
							</h3>
							<p class="text-sm text-fg-2">{spotlightMeta}</p>
						</div>
					</div>
					<p class="mt-5 max-w-110 text-sm text-fg-2">
						{spotlight.description || 'Tu colección, siempre a mano.'}
					</p>
					<div class="mt-5 flex gap-2.5">
						<Button variant="accent" onclick={playSpotlight}>Reproducir</Button>
						<Button variant="secondary" onclick={shuffleSpotlight}>Aleatorio</Button>
					</div>
				</div>
				<div class="flex flex-col gap-0.5 p-5.5 sm:p-6">
					{#each spotlightRows as { target, seconds }, i (targetId(target))}
						<div
							role="button"
							tabindex="0"
							onclick={() => playSpotlightTrack(i)}
							onkeydown={(e) => {
								if (e.key !== 'Enter' && e.key !== ' ') return;
								e.preventDefault();
								playSpotlightTrack(i);
							}}
							class="flex items-center gap-3 rounded-control p-2 text-left transition-colors hover:bg-hover {isTargetCurrent(
								target
							)
								? 'bg-accent-tint'
								: ''}"
						>
							<span class="w-5 shrink-0 text-center text-xs text-muted tabular-nums">{i + 1}</span>
							<div class="min-w-0 flex-1">
								<div
									class="truncate text-sm {isTargetCurrent(target)
										? 'text-accent-soft'
										: 'text-fg'}"
								>
									{targetTitle(target)}
								</div>
								<ArtistLink
									name={targetArtist(target)}
									ownerUserId={targetOwnerUserId(target)}
									class="mt-0.5 text-xs text-fg-3"
								/>
							</div>
							<span class="shrink-0 text-xs text-muted tabular-nums">{fmtTime(seconds)}</span>
						</div>
					{:else}
						<p class="p-2 text-sm text-fg-3">Esta playlist todavía no tiene canciones.</p>
					{/each}
				</div>
			</div>
		</section>
	{/if}

	<!-- MIXES -->
	<section>
		<SectionHeading title="Mixes" subtitle="Generados a partir de lo que más repites" />
		{#if mixes.length > 0}
			<div class="grid grid-cols-[repeat(auto-fill,minmax(164px,1fr))] gap-5">
				{#each mixes as mix, i (mix.id)}
					<MixTile {mix} index={i} />
				{/each}
			</div>
		{:else}
			<EmptyState
				icon={Shuffle}
				title="Todavía no tienes mezclas"
				description="Se generan automáticamente a partir de lo que escuchas."
			/>
		{/if}
	</section>

	<!-- POPULARES ESTA SEMANA -->
	<section>
		<SectionHeading title="Populares esta semana" subtitle="Lo más escuchado de tu biblioteca" />
		{#if popularTargets.length > 0}
			<div class="grid grid-cols-1 gap-x-8.5 gap-y-1.5 lg:grid-cols-2">
				{#each popularTargets as target, i (targetId(target))}
					{@const isCurrent = isTargetCurrent(target)}
					<div
						role="button"
						tabindex="0"
						class="flex items-center gap-3.5 rounded-control p-2 transition-colors {isCurrent
							? 'bg-accent-tint'
							: 'hover:bg-hover'}"
						onclick={() => playPopular(i)}
						onkeydown={(e) => onRowKeydown(e, () => playPopular(i))}
						oncontextmenu={(e) => openContextMenu(e, target)}
					>
						<span class="flex h-3.5 w-4.5 shrink-0 items-end justify-center">
							{#if isCurrent}
								<EqBars size={13} paused={!player.playing} />
							{:else}
								<span class="text-xs text-muted tabular-nums">{i + 1}</span>
							{/if}
						</span>
						<div class="min-w-0 flex-1">
							<TrackTitleCell
								trackId={targetId(target)}
								title={targetTitle(target)}
								artist={targetArtist(target)}
								ownerUserId={targetOwnerUserId(target)}
								coverSize="h-10 w-10"
								explicit={targetExplicit(target)}
								active={isCurrent}
								onClick={() => playPopular(i)}
							/>
						</div>
						<span class="hidden shrink-0 text-xs text-muted tabular-nums sm:block">
							{fmtPlays(targetListensCount(target))}
						</span>
					</div>
				{/each}
			</div>
		{:else}
			<EmptyState
				icon={History}
				title="Todavía no has escuchado nada"
				description="Reproduce alguna canción y tu actividad reciente aparecerá aquí."
			>
				{#snippet actions()}
					<Button href="/explore" variant="secondary">Explorar música</Button>
				{/snippet}
			</EmptyState>
		{/if}
	</section>

	<!-- TUS PLAYLISTS -->
	<section>
		<SectionHeading title="Tus playlists" subtitle="Creadas y guardadas por ti">
			{#snippet actions()}
				{#if data.playlistsHasMore}
					<a
						href="/playlists"
						class="shrink-0 text-xs font-medium tracking-wider text-fg-3 uppercase transition-colors hover:text-fg"
					>
						Ver todas
					</a>
				{/if}
			{/snippet}
		</SectionHeading>
		{#if playlists.length > 0}
			<div class="grid grid-cols-[repeat(auto-fill,minmax(258px,1fr))] gap-3.5">
				{#each playlists as playlist, i (playlist.id)}
					<div
						role="button"
						tabindex="0"
						class="group/card animate-enter flex items-center gap-3.5 rounded-2xl p-3 transition-colors hover:bg-hover"
						style="animation-delay:{Math.min(i, 10) * 40}ms"
						onclick={() => goto(`/playlists/${playlist.id}`)}
						onkeydown={(e) => onRowKeydown(e, () => goto(`/playlists/${playlist.id}`))}
					>
						<PlaylistArt
							playlistId={playlist.id}
							trackIds={playlist.coverTrackIds}
							version={playlist.updatedAt}
							size="medium"
							class="h-15.5 w-15.5 shrink-0 rounded-control"
						/>
						<div class="min-w-0 flex-1">
							<div class="truncate text-sm font-medium tracking-tight text-fg">
								{playlist.name}
							</div>
							{#if playlist.description}
								<div class="mt-1 truncate text-xs text-fg-3">{playlist.description}</div>
							{/if}
							<ArtistLink
								name={data.user?.name}
								ownerUserId={data.user?.sub}
								class="mt-1 text-xs text-fg-3"
							/>
						</div>
					</div>
				{/each}
			</div>
		{:else}
			<EmptyState
				icon={ListMusic}
				title="Todavía no tienes listas"
				description="Crea tu primera lista y añádele canciones de tu biblioteca."
			>
				{#snippet actions()}
					<Button href="/playlists">Crear lista</Button>
				{/snippet}
			</EmptyState>
		{/if}
	</section>
</div>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		{playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={addToQueue}
	/>
{/if}
