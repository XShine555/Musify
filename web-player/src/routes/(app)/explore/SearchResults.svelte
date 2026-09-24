<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player } from '$lib/player/player.svelte';
	import type { Album, Playlist, Track } from '$lib/types';
	import { fmtTime, fmtPlays, plural } from '$lib/utils/format';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/primitives/InfiniteScroll.svelte';
	import UserRow from '$lib/components/ui/media/UserRow.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import SectionHeading from '$lib/components/ui/layout/SectionHeading.svelte';
	import { playlistCover } from '$lib/utils/hrefs';
	import type { Menu } from '$lib/state/menu.svelte';
	import type { PagedList } from '$lib/state/pagedList.svelte';
	import {
		type SearchCounts,
		type SearchFilter,
		showGroup,
		capped,
		SEARCH_LABELS
	} from '$lib/data/search';

	interface ResultUser {
		id: string;
		name: string;
		profilePictureUrl: string | null;
	}

	interface Props {
		filter: SearchFilter;
		counts: SearchCounts;
		query: string;
		inGenre: boolean;
		tracksList: PagedList<Track>;
		albums: Album[];
		playlists: Playlist[];
		users: ResultUser[];
		trackMenu: Menu<Track>;
		albumMenu: Menu<string>;
	}

	let {
		filter,
		counts,
		query,
		inGenre,
		tracksList,
		albums,
		playlists,
		users,
		trackMenu,
		albumMenu
	}: Props = $props();

	const items = $derived(tracksList.items);
</script>

<div class="mt-9 flex flex-col gap-9">
	{#if showGroup(filter, 'tracks')}
		<div>
			{#if items.length > 0}
				<SectionHeading title={SEARCH_LABELS.tracks} count={counts.tracks} />
				<ul class="flex flex-col gap-1">
					{#each capped(filter, items) as track, i (track.id)}
						<li>
							<ListRow
								title={track.title}
								subtitle={track.artist}
								subtitleHref={track.ownerUserId}
								explicit={track.explicit}
								active={player.currentId === track.id}
								size="lg"
								trackId={track.id}
								onclick={() => player.playOrToggle(items, i)}
								oncontextmenu={(e) => trackMenu.open(e, track)}
							>
								{#snippet overlay()}
									{#if player.currentId === track.id}
										<EqBars overlay paused={!player.playing} />
									{/if}
								{/snippet}
								{#snippet trailing()}
									<span class="hidden shrink-0 text-xs text-fg-2 tabular-nums sm:block">
										{fmtTime(track.duration)} · {fmtPlays(track.listensCount)}
									</span>
								{/snippet}
							</ListRow>
						</li>
					{/each}
				</ul>

				{#if filter === 'tracks' && tracksList.hasMore}
					<InfiniteScroll
						onLoadMore={() => tracksList.loadMore()}
						hasMore={tracksList.hasMore}
						loading={tracksList.loading}
						error={tracksList.error}
					/>
				{/if}
			{:else}
				<EmptyState
					icon={Music}
					description={inGenre
						? 'Todavía no hay canciones de este género.'
						: `No hay canciones que coincidan con «${query}».`}
				/>
			{/if}
		</div>
	{/if}

	{#if albums.length > 0 && showGroup(filter, 'albums')}
		<div>
			<SectionHeading title={SEARCH_LABELS.albums} count={counts.albums} />
			<ul class="flex flex-col gap-1">
				{#each capped(filter, albums) as album (album.id)}
					<li>
						<ListRow
							title={album.title}
							href="/albums/{album.id}"
							size="lg"
							trackIds={album.coverTrackIds}
							oncontextmenu={(e) => albumMenu.open(e, album.id)}
						>
							{#snippet trailing()}
								<span class="hidden shrink-0 text-xs text-fg-2 tabular-nums sm:block">
									{plural(album.trackCount, 'canción', 'canciones')}
								</span>
							{/snippet}
						</ListRow>
					</li>
				{/each}
			</ul>
		</div>
	{/if}

	{#if users.length > 0 && showGroup(filter, 'users')}
		<div>
			<SectionHeading title={SEARCH_LABELS.users} count={counts.users} />
			<ul class="flex flex-col gap-1">
				{#each capped(filter, users) as u (u.id)}
					<li>
						<UserRow user={u} />
					</li>
				{/each}
			</ul>
		</div>
	{/if}

	{#if playlists.length > 0 && showGroup(filter, 'playlists')}
		<div>
			<SectionHeading title={SEARCH_LABELS.playlists} count={counts.playlists} />
			<ul class="flex flex-col gap-1">
				{#each capped(filter, playlists) as playlist (playlist.id)}
					<li>
						<ListRow
							title={playlist.name}
							subtitle={playlist.description}
							href="/playlists/{playlist.id}"
							size="lg"
							coverSrc={playlistCover(playlist, 'medium')}
							trackIds={playlist.coverTrackIds}
						/>
					</li>
				{/each}
			</ul>
		</div>
	{/if}
</div>
