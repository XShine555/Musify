<script lang="ts" module>
	const FADE = { duration: 200 };
	const SEARCH_DEBOUNCE_MS = 300;
</script>

<script lang="ts">
	import { tick } from 'svelte';
	import { fade } from 'svelte/transition';
	import { enhance } from '$app/forms';
	import { goto } from '$app/navigation';
	import Upload from '@lucide/svelte/icons/upload';
	import Music from '@lucide/svelte/icons/music';
	import SearchX from '@lucide/svelte/icons/search-x';
	import type { LibraryTrack, Paged, TrackStatusCounts } from '$lib/types';
	import { player } from '$lib/player/player.svelte';
	import {
		canPublish,
		filterCounts,
		TRACK_FILTERS,
		type TrackFilter
	} from '$lib/data/libraryTracks';
	import { createPagedList } from '$lib/state/pagedList.svelte';
	import { plural } from '$lib/utils/format';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import InfiniteScroll from '$lib/components/ui/primitives/InfiniteScroll.svelte';
	import SelectionBar from '$lib/components/ui/primitives/SelectionBar.svelte';
	import SearchInput from '$lib/components/ui/primitives/SearchInput.svelte';
	import ChipGroup from '$lib/components/ui/primitives/ChipGroup.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import ManageTrackList from '$lib/components/music/library/ManageTrackList.svelte';

	interface Props {
		tracks: Paged<LibraryTrack>;
		counts: TrackStatusCounts;
		query: string;
		filter: TrackFilter;
		errorMessage?: string;
		notice?: string;
	}

	let { tracks, counts, query, filter, errorMessage, notice }: Props = $props();

	const list = createPagedList<LibraryTrack>(
		async (pageNumber) => {
			const params = new URLSearchParams({ pageNumber: String(pageNumber), status: filter });
			if (query) params.set('q', query);
			const res = await fetch(`/api/library/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			return (await res.json()) as Paged<LibraryTrack>;
		},
		(track) => track.id
	);

	let search = $state('');
	let selectedIds = $state<string[]>([]);
	let pendingDelete = $state<LibraryTrack[]>([]);
	let pendingUnpublish = $state<LibraryTrack[]>([]);
	let publishForm = $state<HTMLFormElement>();
	let publishIds = $state('');
	let publishValue = $state('true');
	let timer: ReturnType<typeof setTimeout> | undefined;

	$effect(() => {
		list.reset(tracks);
		search = query;
		const ids = new Set(list.items.map((track) => track.id));
		selectedIds = selectedIds.filter((id) => ids.has(id));
	});

	const items = $derived(list.items);
	const hasFilters = $derived(query !== '' || filter !== 'all');
	const counters = $derived(filterCounts(counts));
	const filterOptions = $derived(
		TRACK_FILTERS.map(({ value, label }) => ({ value, label, count: counters[value] }))
	);
	const selected = $derived(items.filter((track) => selectedIds.includes(track.id)));
	const pendingTitle = (group: LibraryTrack[]) =>
		group.length === 1 ? `«${group[0].title}»` : plural(group.length, 'canción', 'canciones');

	function navigate(nextQuery: string, nextFilter: TrackFilter) {
		const params = new URLSearchParams();
		if (nextQuery.trim()) params.set('q', nextQuery.trim());
		if (nextFilter !== 'all') params.set('status', nextFilter);
		const queryString = params.toString();
		goto(queryString ? `?${queryString}` : '?', {
			keepFocus: true,
			noScroll: true,
			replaceState: true
		});
	}

	function onSearch() {
		clearTimeout(timer);
		timer = setTimeout(() => navigate(search, filter), SEARCH_DEBOUNCE_MS);
	}

	function togglePlay(index: number) {
		player.playOrToggle(items, index);
	}

	async function publish(group: LibraryTrack[]) {
		const ready = group.filter((track) => canPublish(track) && !track.published);
		if (ready.length === 0) return;
		publishIds = ready.map((track) => track.id).join(',');
		publishValue = 'true';
		await tick();
		publishForm?.requestSubmit();
	}

	function requestToggle(track: LibraryTrack) {
		if (track.published) pendingUnpublish = [track];
		else publish([track]);
	}

	function unpublishSelected() {
		pendingUnpublish = selected.filter((track) => track.published);
	}
</script>

{#if errorMessage}
	<Alert tone="danger" class="mt-4">{errorMessage}</Alert>
{:else if notice}
	<Alert class="mt-4">{notice}</Alert>
{/if}

<form
	bind:this={publishForm}
	method="POST"
	action="?/setPublished"
	class="hidden"
	use:enhance={() =>
		async ({ update }) => {
			await update({ reset: false });
			selectedIds = [];
		}}
>
	<input type="hidden" name="trackIds" value={publishIds} />
	<input type="hidden" name="isPublished" value={publishValue} />
</form>

{#if counts.total > 0}
	<div class="mt-4 grid">
		{#if selectedIds.length > 0}
			<div class="col-start-1 row-start-1" in:fade={FADE} out:fade={FADE}>
				<SelectionBar
					label={plural(selectedIds.length, 'seleccionada', 'seleccionadas')}
					onClear={() => (selectedIds = [])}
				>
					<Button size="sm" variant="secondary" onclick={() => publish(selected)}>Publicar</Button>
					<Button size="sm" variant="secondary" onclick={unpublishSelected}>Despublicar</Button>
					<Button size="sm" variant="danger" onclick={() => (pendingDelete = selected)}>
						Borrar
					</Button>
				</SelectionBar>
			</div>
		{:else}
			<div
				class="col-start-1 row-start-1 flex min-h-11 flex-wrap items-center gap-x-4 gap-y-3"
				in:fade={FADE}
				out:fade={FADE}
			>
				<SearchInput
					bind:value={search}
					oninput={onSearch}
					placeholder="Filtrar por título"
					size="sm"
					class="w-full sm:max-w-search sm:flex-1"
				/>
				<ChipGroup
					label="Filtrar por estado"
					options={filterOptions}
					bind:value={() => filter, (value) => navigate(search, value)}
				/>
			</div>
		{/if}
	</div>

	{#if items.length > 0}
		<ManageTrackList
			class="mt-4"
			tracks={items}
			bind:selectedIds
			onPlay={togglePlay}
			onPublish={requestToggle}
			onDelete={(track) => (pendingDelete = [track])}
		/>
		<InfiniteScroll
			onLoadMore={() => list.loadMore()}
			hasMore={list.hasMore}
			loading={list.loading}
			error={list.error}
		/>
	{:else}
		<EmptyState
			icon={SearchX}
			title="Sin resultados"
			description={hasFilters
				? 'Ninguna canción coincide con la búsqueda o el filtro.'
				: 'No hay canciones en esta lista.'}
		/>
	{/if}
{:else}
	<EmptyState
		icon={Music}
		title="Todavía no tienes canciones"
		description="Sube la primera y aparecerá aquí lista para reproducir."
	>
		{#snippet actions()}
			<Button href="/library/uploads">
				<Upload class="size-icon-sm" />
				Sube la primera
			</Button>
		{/snippet}
	</EmptyState>
{/if}

<ConfirmDialog
	open={pendingDelete.length > 0}
	onClose={() => (pendingDelete = [])}
	title="¿Borrar {pendingTitle(pendingDelete)}?"
	description="Esta acción no se puede deshacer: se eliminarán de tu biblioteca, de tus álbumes y de las playlists donde estén."
	confirmLabel="Borrar"
	action="?/deleteTracks"
	fields={{ trackIds: pendingDelete.map((track) => track.id).join(',') }}
/>

<ConfirmDialog
	open={pendingUnpublish.length > 0}
	onClose={() => (pendingUnpublish = [])}
	title="¿Despublicar {pendingTitle(pendingUnpublish)}?"
	description="Dejarán de verse en álbumes, playlists, búsquedas y perfiles. Solo tú podrás verlas y reproducirlas."
	confirmLabel="Despublicar"
	action="?/setPublished"
	fields={{ trackIds: pendingUnpublish.map((track) => track.id).join(','), isPublished: 'false' }}
/>
