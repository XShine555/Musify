<script lang="ts">
	import { playlistCover } from '$lib/utils/hrefs';
	import History from '@lucide/svelte/icons/history';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import { player } from '$lib/player/player.svelte';
	import { playShuffled } from '$lib/player/actions';
	import type { Track } from '$lib/types';
	import { mergeRecentlyPlayed } from '$lib/player/recentlyPlayed';
	import { fetchPlaylistTracks } from '$lib/data/albums';
	import { fmtTime, fmtDurationLong, plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import SectionHeading from '$lib/components/ui/layout/SectionHeading.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import PlayAllButton from '$lib/components/music/PlayAllButton.svelte';
	import TrackContextMenu from '$lib/components/music/TrackContextMenu.svelte';
	import { createMenu } from '$lib/state/menu.svelte';
	import { HOME_CONTINUE_LIMIT } from '$lib/config';

	let { data } = $props();

	const trackMenu = createMenu<Track>();

	const spotlight = $derived(data.spotlightPlaylist);
	const continueItems = $derived(mergeRecentlyPlayed(data.recentlyPlayed, HOME_CONTINUE_LIMIT));

	async function shuffleSpotlight() {
		if (data.spotlightPlaylist) playShuffled(await fetchPlaylistTracks(data.spotlightPlaylist.id));
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
						{#await data.spotlightTracks then { items }}
							<PlayAllButton
								{items}
								load={() => fetchPlaylistTracks(spotlight.id)}
								size="md"
								idleLabel="Reanudar {spotlight.name}"
								activeLabel="Pausar {spotlight.name}"
							/>
						{/await}
					{/if}
					<Button href="/explore" variant="secondary">Explorar música</Button>
				</div>
			</div>

			{#await data.listeningStats then stats}
				{@const statsList = [
					{ value: stats.tracksThisWeek, label: 'Canciones nuevas escuchadas esta semana' },
					{
						value: fmtDurationLong(stats.secondsThisWeek),
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
			<SectionHeading title="Continuar escuchando" subtitle="Retómalo donde lo dejaste" />
			{#if continueItems.length > 0}
				<div class="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-5">
					{#each continueItems as item, i (item.id)}
						{@const isCurrent = player.currentId === item.id}
						<ListRow
							onclick={() => player.playOrToggle(continueItems, i)}
							oncontextmenu={(e) => trackMenu.open(e, item)}
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
				{#await data.spotlightTracks then { items: previewItems, totalCount }}
					{@const visibilityLabel = spotlight.visibility === 'Public' ? 'Pública' : 'Privada'}
					{@const meta =
						totalCount === 0
							? visibilityLabel
							: `${plural(totalCount, 'canción', 'canciones')} · ${fmtDurationLong(spotlight.durationSeconds)} · ${visibilityLabel}`}
					<div
						class="grid spotlight-surface grid-cols-1 overflow-hidden rounded-panel-lg lg:grid-cols-2"
					>
						<div class="flex flex-col justify-center p-8 sm:p-9">
							<a
								href="/playlists/{spotlight.id}"
								class="flex w-fit items-center gap-5 self-start sm:gap-6"
							>
								<Artwork
									src={playlistCover(spotlight, 'large')}
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
									<p class="text-sm text-fg-2">{meta}</p>
								</div>
							</a>
							<p class="mt-5 max-w-prose-sm text-sm text-fg-2">
								{spotlight.description || 'Tu colección, siempre a mano.'}
							</p>
							<div class="mt-5 flex gap-2.5">
								<PlayAllButton
									items={previewItems}
									load={() => fetchPlaylistTracks(spotlight.id)}
									variant="accent"
									size="md"
								/>
								<Button variant="secondary" onclick={shuffleSpotlight}>Aleatorio</Button>
							</div>
						</div>
						<div class="flex flex-col gap-2 p-5.5 sm:p-6">
							{#each previewItems as track, i (track.id)}
								<ListRow
									onclick={() => player.playOrToggle(previewItems, i)}
									active={player.currentId === track.id}
									size="sm"
									title={track.title}
									subtitle={track.artist}
									subtitleHref={track.ownerUserId}
									explicit={track.explicit}
								>
									{#snippet leading()}
										<span class="w-5 shrink-0 text-center text-xs text-fg-2 tabular-nums"
											>{i + 1}</span
										>
									{/snippet}
									{#snippet trailing()}
										<span class="shrink-0 text-xs text-fg-2 tabular-nums"
											>{fmtTime(track.duration)}</span
										>
									{/snippet}
								</ListRow>
							{:else}
								<p class="p-2 text-sm text-fg-2">Esta playlist todavía no tiene canciones.</p>
							{/each}
						</div>
					</div>
				{/await}
			</section>
		{/if}

		<section>
			<SectionHeading title="Mezclas" subtitle="Generados a partir de lo que más repites" />
			{#if data.mixes.length > 0}
				<div class="grid-tiles">
					{#each data.mixes as mix, i (mix.id)}
						<MediaCard
							href="/mixes/{mix.id}"
							title={mix.title}
							subtitle={mix.subtitle}
							trackIds={mix.tracks.map((track) => track.id)}
							onPlay={(event) => {
								event.preventDefault();
								player.playQueue(mix.tracks, 0);
							}}
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
			<SectionHeading
				title="Tus playlists"
				subtitle="Creadas y guardadas por ti"
				href={data.playlistsHasMore ? '/playlists' : undefined}
			/>
			{#if data.playlists.length > 0}
				<div class="grid-wide">
					{#each data.playlists as playlist, i (playlist.id)}
						<ListRow
							href="/playlists/{playlist.id}"
							size="lg"
							variant="card"
							title={playlist.name}
							subtitle={playlist.description}
							coverSrc={playlistCover(playlist, 'medium')}
							trackIds={playlist.coverTrackIds}
							style="--i:{i}"
						/>
					{/each}
				</div>
			{:else}
				<EmptyState
					icon={ListMusic}
					title="Todavía no tienes playlists"
					description="Crea tu primera playlist y añádele canciones de tu biblioteca."
				>
					{#snippet actions()}
						<Button href="/playlists">Crear playlist</Button>
					{/snippet}
				</EmptyState>
			{/if}
		</section>
	</div>
</Page>

<TrackContextMenu menu={trackMenu} playlists={data.userPlaylists} />
