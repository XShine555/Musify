<script lang="ts">
	import { goto } from '$app/navigation';
	import History from '@lucide/svelte/icons/history';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import { player, isQueueCurrent, playAllOrToggle, playShuffled } from '$lib/player/player.svelte';
	import { pressable } from '$lib/actions/pressable';
	import { mergeRecentlyPlayed } from '$lib/recentlyPlayed';
	import { mixItemTarget } from '$lib/mixes';
	import { fmtTime, fmtDurationLong, fmtPlays, plural } from '$lib/format';
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
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import MediaCard from '$lib/components/ui/MediaCard.svelte';
	import EqBars from '$lib/components/ui/EqBars.svelte';
	import ListRow from '$lib/components/ui/ListRow.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import ContextMenu from '$lib/components/ui/ContextMenu.svelte';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { createTrackMenu } from '$lib/menus.svelte';
	import { HOME_CONTINUE_LIMIT, HOME_POPULAR_MAX } from '$lib/config';

	let { data } = $props();

	const trackMenu = createTrackMenu();

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
					plural(spotlightTrackCount, 'canción', 'canciones'),
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
	const spotlightIsCurrent = $derived(isQueueCurrent(spotlightQueue));
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
		playAllOrToggle(spotlightQueue);
	}
	function shuffleSpotlight() {
		playShuffled(spotlightQueue);
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		trackMenu.open(event, target);
	}

	function playMix(mix: (typeof mixes)[number], event: MouseEvent) {
		event.preventDefault();
		player.playQueue(
			mix.items.map((item) => queueItemForTarget(mixItemTarget(item))),
			0
		);
	}
</script>

{#snippet noListeningHistory()}
	<EmptyState
		icon={History}
		title="Todavía no has escuchado nada"
		description="Reproduce alguna canción y tu actividad reciente aparecerá aquí."
	>
		{#snippet actions()}
			<Button href="/explore" variant="secondary">Explorar música</Button>
		{/snippet}
	</EmptyState>
{/snippet}

<svelte:head>
	<title>Musify</title>
	<meta name="description" content="Tu música, sin límites." />
</svelte:head>

<section class="page-x pt-3 sm:pt-4">
	<div
		class="relative overflow-hidden rounded-3xl bg-[image:var(--mf-hero-bg)] px-6 py-8 theme-transition sm:px-9 sm:py-8.5"
	>
		<div
			class="relative flex flex-col items-start gap-7 lg:flex-row lg:flex-wrap lg:items-end lg:justify-between"
		>
			<div class="animate-enter max-w-hero min-w-65">
				<h1
					class="font-display text-3xl leading-tight font-medium tracking-tight text-pretty text-fg sm:text-4xl"
				>
					{data.greeting}
				</h1>
				<p class="mt-3.5 max-w-prose-sm text-body text-fg-2">
					Sube lo tuyo, descubre lo nuevo y escúchalo todo en un solo sitio.
				</p>
				<div class="mt-6.5 flex flex-wrap items-center gap-2.5">
					{#if spotlight}
						<Button variant="strong" onclick={playSpotlight}>
							{heroPlaying ? 'Pausar' : 'Reanudar'}
							{spotlight.name}
						</Button>
					{/if}
					<Button href="/explore" variant="glass">Explorar música</Button>
				</div>
			</div>

			{#await data.listeningStats then stats}
				<div class="flex gap-6.5">
					<div>
						<div class="text-display-2 text-fg">
							{stats.tracksThisWeek}
						</div>
						<div class="mt-1 text-xs text-fg-3">Canciones nuevas escuchadas esta semana</div>
					</div>
					<div>
						<div class="text-display-2 text-fg">
							{fmtDurationLong(Number(stats.secondsThisWeek))}
						</div>
						<div class="mt-1 text-xs text-fg-3">Tiempo de escucha esta semana</div>
					</div>
					<div>
						<div class="text-display-2 text-fg">
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
					<ListRow
						onclick={() => playContinue(i)}
						oncontextmenu={(e) => openContextMenu(e, targetForQueueItem(item))}
						active={isCurrent}
						size="lg"
						variant="card"
						title={item.title}
						subtitle={item.artist}
						subtitleHref={item.ownerUserId}
						trackId={item.id}
						class="animate-enter"
						style="--i:{i}"
					/>
				{/each}
			</div>
		{:else}
			{@render noListeningHistory()}
		{/if}
	</section>

	<!-- PLAYLIST DESTACADA -->
	{#if spotlight}
		<section>
			<div
				class="grid grid-cols-1 overflow-hidden rounded-panel-lg theme-transition lg:grid-cols-2"
				style="background-image:var(--mf-spotlight-bg), linear-gradient(to right, transparent 50%, color-mix(in oklch, var(--mf-scrim) 22%, transparent) 61%, transparent 92.5%)"
			>
				<div class="flex flex-col justify-center p-8 sm:p-9">
					<div
						role="button"
						tabindex="0"
						use:pressable={() => goto(`/playlists/${spotlight.id}`)}
						class="flex w-fit items-center gap-5 self-start sm:gap-6"
					>
						<Artwork
							src="/api/playlists/{spotlight.id}/cover?size=large&v={encodeURIComponent(
								spotlight.updatedAt
							)}"
							trackIds={spotlight.coverTrackIds}
							size="2xl"
							alt={spotlight.name}
							class="shrink-0"
						/>
						<div class="flex min-w-0 flex-col gap-3">
							<p class="text-eyebrow text-fg-2">Playlist destacada</p>
							<h3 class="truncate text-display-2 text-fg">
								{spotlight.name}
							</h3>
							<p class="text-sm text-fg-2">{spotlightMeta}</p>
						</div>
					</div>
					<p class="mt-5 max-w-prose-sm text-sm text-fg-2">
						{spotlight.description || 'Tu colección, siempre a mano.'}
					</p>
					<div class="mt-5 flex gap-2.5">
						<Button variant="accent" onclick={playSpotlight}>Reproducir</Button>
						<Button variant="glass" onclick={shuffleSpotlight}>Aleatorio</Button>
					</div>
				</div>
				<div class="flex flex-col gap-0.5 p-5.5 sm:p-6">
					{#each spotlightRows as { target, seconds }, i (targetId(target))}
						<ListRow
							onclick={() => playSpotlightTrack(i)}
							active={isTargetCurrent(target)}
							size="sm"
							title={targetTitle(target)}
							subtitle={targetArtist(target)}
							subtitleHref={targetOwnerUserId(target)}
						>
							{#snippet leading()}
								<span class="w-5 shrink-0 text-center text-xs text-muted tabular-nums">{i + 1}</span
								>
							{/snippet}
							{#snippet trailing()}
								<span class="shrink-0 text-xs text-muted tabular-nums">{fmtTime(seconds)}</span>
							{/snippet}
						</ListRow>
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
			<div class="grid-tiles">
				{#each mixes as mix, i (mix.id)}
					<MediaCard
						href="/mixes/{mix.id}"
						title={mix.title}
						subtitle={mix.subtitle}
						trackIds={mix.items.map((item) => item.trackId)}
						onPlay={(event) => playMix(mix, event)}
						index={i}
					/>
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
					<ListRow
						onclick={() => playPopular(i)}
						oncontextmenu={(e) => openContextMenu(e, target)}
						active={isCurrent}
						size="sm"
						title={targetTitle(target)}
						subtitle={targetArtist(target)}
						subtitleHref={targetOwnerUserId(target)}
						explicit={targetExplicit(target)}
						trackId={targetId(target)}
					>
						{#snippet leading()}
							<span class="flex h-3.5 w-4.5 shrink-0 items-end justify-center">
								{#if isCurrent}
									<EqBars size={13} paused={!player.playing} />
								{:else}
									<span class="text-xs text-muted tabular-nums">{i + 1}</span>
								{/if}
							</span>
						{/snippet}
						{#snippet trailing()}
							<span class="hidden shrink-0 text-xs text-muted tabular-nums sm:block">
								{fmtPlays(targetListensCount(target))}
							</span>
						{/snippet}
					</ListRow>
				{/each}
			</div>
		{:else}
			{@render noListeningHistory()}
		{/if}
	</section>

	<!-- TUS PLAYLISTS -->
	<section>
		<SectionHeading
			title="Tus playlists"
			subtitle="Creadas y guardadas por ti"
			href={data.playlistsHasMore ? '/playlists' : undefined}
		/>
		{#if playlists.length > 0}
			<div class="grid-wide">
				{#each playlists as playlist, i (playlist.id)}
					<ListRow
						onclick={() => goto(`/playlists/${playlist.id}`)}
						size="lg"
						variant="card"
						title={playlist.name}
						subtitle={playlist.description}
						coverSrc="/api/playlists/{playlist.id}/cover?size=medium&v={encodeURIComponent(
							playlist.updatedAt
						)}"
						trackIds={playlist.coverTrackIds}
						style="--i:{i}"
					/>
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
		{playlists}
	/>
{/if}
