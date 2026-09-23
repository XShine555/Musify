<script lang="ts">
	import { goto } from '$app/navigation';
	import History from '@lucide/svelte/icons/history';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import {
		player,
		isQueueCurrent,
		playAllOrToggle,
		playShuffled,
		toQueueItem,
		trackFromQueueItem,
		type ApiTrackLike
	} from '$lib/player/player.svelte';
	import { pressable } from '$lib/actions/pressable';
	import { mergeRecentlyPlayed } from '$lib/data/recentlyPlayed';
	import { mixItemTrack } from '$lib/data/mixes';
	import { fmtTime, fmtDurationLong, fmtPlays, plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import SectionHeading from '$lib/components/ui/layout/SectionHeading.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import ContextMenu from '$lib/components/ui/overlay/ContextMenu.svelte';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { createTrackMenu } from '$lib/state/menus.svelte';
	import { HOME_CONTINUE_LIMIT, HOME_POPULAR_MAX } from '$lib/config';

	let { data } = $props();

	const trackMenu = createTrackMenu();

	type SpotlightTrack = Awaited<typeof data.spotlightTracks>['items'][number];
	type SpotlightRow = { track: SpotlightTrack; seconds: number };
	let spotlightRows = $state<SpotlightRow[]>([]);
	let spotlightTotal = $state(0);

	$effect(() => {
		spotlightRows = [];
		spotlightTotal = 0;
		let cancelled = false;
		data.spotlightTracks.then(({ items, totalCount }) => {
			if (cancelled) return;
			spotlightRows = items.map((track) => ({
				track,
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
	const spotlightItems = $derived(spotlightRows.map((row) => row.track));
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

	const popularTracks = $derived<ApiTrackLike[]>(
		recentPool.map(trackFromQueueItem).slice(0, HOME_POPULAR_MAX)
	);

	const spotlightQueue = $derived(spotlightItems.map(toQueueItem));
	const spotlightIsCurrent = $derived(isQueueCurrent(spotlightQueue));
	const heroPlaying = $derived(spotlightIsCurrent && player.playing);

	function playContinue(index: number) {
		player.playOrToggle(continueItems, index);
	}
	function playPopular(index: number) {
		player.playOrToggle(popularTracks.map(toQueueItem), index);
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

	function openContextMenu(event: MouseEvent, track: ApiTrackLike) {
		trackMenu.open(event, track);
	}

	function playMix(mix: (typeof mixes)[number], event: MouseEvent) {
		event.preventDefault();
		player.playQueue(
			mix.items.map((item) => toQueueItem(mixItemTrack(item))),
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

<Page>
	<div class="relative hero-surface overflow-hidden rounded-panel-lg px-6 py-8 sm:px-9 sm:py-8.5">
		<div
			class="relative flex flex-col items-start gap-7 lg:flex-row lg:flex-wrap lg:items-end lg:justify-between"
		>
			<div class="animate-enter max-w-hero min-w-65 dark:shadow-accent">
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
						<Button variant="primary" onclick={playSpotlight}>
							{heroPlaying ? 'Pausar' : 'Reanudar'}
							{spotlight.name}
						</Button>
					{/if}
					<Button href="/explore" variant="secondary">Explorar música</Button>
				</div>
			</div>

			{#await data.listeningStats then stats}
				{@const statsList = [
					{ value: stats.tracksThisWeek, label: 'Canciones nuevas escuchadas esta semana' },
					{
						value: fmtDurationLong(Number(stats.secondsThisWeek)),
						label: 'Tiempo de escucha esta semana'
					},
					{ value: stats.streakDays, label: 'Días consecutivos escuchando' }
				]}
				<div class="flex gap-6.5">
					{#each statsList as stat (stat.label)}
						<div>
							<div class="text-display-2 text-fg">{stat.value}</div>
							<div class="mt-1 text-xs text-fg-2">{stat.label}</div>
						</div>
					{/each}
				</div>
			{/await}
		</div>
	</div>

	<div class="mt-10 flex flex-col gap-section">
		<section>
			<SectionHeading title="Continuar escuchando" subtitle="Retomalo donde lo dejaste" />
			{#if continueItems.length > 0}
				<div class="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-5">
					{#each continueItems as item, i (item.id)}
						{@const isCurrent = player.current.id === item.id}
						<ListRow
							onclick={() => playContinue(i)}
							oncontextmenu={(e) => openContextMenu(e, trackFromQueueItem(item))}
							active={isCurrent}
							size="lg"
							variant="card"
							title={item.title}
							subtitle={item.artist}
							subtitleHref={item.ownerUserId}
							explicit={item.explicit}
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

		{#if spotlight}
			<section>
				<div
					class="grid spotlight-surface grid-cols-1 overflow-hidden rounded-panel-lg lg:grid-cols-2"
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
							<Button variant="secondary" onclick={shuffleSpotlight}>Aleatorio</Button>
						</div>
					</div>
					<div class="flex flex-col gap-2 p-5.5 sm:p-6">
						{#each spotlightRows as { track, seconds }, i (track.id)}
							<ListRow
								onclick={() => playSpotlightTrack(i)}
								active={player.current.id === track.id}
								size="sm"
								title={track.title}
								subtitle={track.artist}
								subtitleHref={track.ownerUserId}
								explicit={track.isExplicit}
							>
								{#snippet leading()}
									<span class="w-5 shrink-0 text-center text-xs text-fg-2 tabular-nums"
										>{i + 1}</span
									>
								{/snippet}
								{#snippet trailing()}
									<span class="shrink-0 text-xs text-fg-2 tabular-nums">{fmtTime(seconds)}</span>
								{/snippet}
							</ListRow>
						{:else}
							<p class="p-2 text-sm text-fg-2">Esta playlist todavía no tiene canciones.</p>
						{/each}
					</div>
				</div>
			</section>
		{/if}

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

		<section>
			<SectionHeading title="Populares esta semana" subtitle="Lo más escuchado de tu biblioteca" />
			{#if popularTracks.length > 0}
				<div class="grid grid-cols-1 gap-x-8.5 gap-y-1.5 lg:grid-cols-2">
					{#each popularTracks as track, i (track.id)}
						{@const isCurrent = player.current.id === track.id}
						<ListRow
							onclick={() => playPopular(i)}
							oncontextmenu={(e) => openContextMenu(e, track)}
							active={isCurrent}
							size="sm"
							title={track.title}
							subtitle={track.artist}
							subtitleHref={track.ownerUserId}
							explicit={track.isExplicit}
							trackId={track.id}
						>
							{#snippet leading()}
								<span class="flex h-3.5 w-icon-md shrink-0 items-end justify-center">
									{#if isCurrent}
										<EqBars size={13} paused={!player.playing} />
									{:else}
										<span class="text-xs text-fg-2 tabular-nums">{i + 1}</span>
									{/if}
								</span>
							{/snippet}
							{#snippet trailing()}
								<span class="hidden shrink-0 text-xs text-fg-2 tabular-nums sm:block">
									{fmtPlays(track.listensCount)}
								</span>
							{/snippet}
						</ListRow>
					{/each}
				</div>
			{:else}
				{@render noListeningHistory()}
			{/if}
		</section>

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
		{playlists}
	/>
{/if}
