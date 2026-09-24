<script lang="ts" module>
	const FADE = { duration: 200 };
	const SEARCH_DEBOUNCE_MS = 300;
	const REFRESH_MIN_MS = 3000;
	const REFRESH_MAX_MS = 30000;
</script>

<script lang="ts">
	import { tick, untrack } from 'svelte';
	import type { SubmitFunction } from '@sveltejs/kit';
	import { fade } from 'svelte/transition';
	import { enhance } from '$app/forms';
	import { goto, invalidateAll } from '$app/navigation';
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

	let hiddenIds = $state<string[]>([]);
	let overrides = $state<Record<string, boolean>>({});
	let deleteError = $state('');
	let announcement = $state('');
	let lastKey = '';
	let previousProcessing = untrack(() => counts.processing);

	$effect(() => {
		const key = `${query}|${filter}|${tracks.totalItemCount}`;
		untrack(() => {
			if (key !== lastKey) {
				lastKey = key;
				list.reset(tracks);
			} else {
				list.merge(tracks.items);
			}
		});
		search = query;
	});

	const items = $derived(
		list.items
			.filter((track) => !hiddenIds.includes(track.id))
			.map((track) =>
				track.id in overrides ? { ...track, published: overrides[track.id] } : track
			)
	);

	$effect(() => {
		const ids = new Set(items.map((track) => track.id));
		if (selectedIds.some((id) => !ids.has(id)))
			selectedIds = selectedIds.filter((id) => ids.has(id));
	});

	const processing = $derived(
		counts.processing > 0 ||
			list.items.some((track) => track.status === 'Pending' || track.status === 'Processing')
	);

	$effect(() => {
		if (!processing) return;
		let delay = REFRESH_MIN_MS;
		let stopped = false;
		let handle: ReturnType<typeof setTimeout>;
		const refresh = async () => {
			if (document.visibilityState === 'visible') await invalidateAll();
			delay = Math.min(delay * 1.5, REFRESH_MAX_MS);
			if (!stopped) handle = setTimeout(refresh, delay);
		};
		handle = setTimeout(refresh, delay);
		return () => {
			stopped = true;
			clearTimeout(handle);
		};
	});

	$effect(() => {
		const now = counts.processing;
		if (now < previousProcessing) {
			announcement = `${plural(previousProcessing - now, 'canción terminó', 'canciones terminaron')} de procesarse.`;
		}
		previousProcessing = now;
	});

	const deleteSubmit: SubmitFunction = () => {
		const ids = pendingDelete.map((track) => track.id);
		hiddenIds = [...hiddenIds, ...ids];
		deleteError = '';
		return async ({ result, update }) => {
			if (result.type === 'failure') {
				hiddenIds = hiddenIds.filter((id) => !ids.includes(id));
				deleteError =
					(result.data as { message?: string } | undefined)?.message ??
					'No se pudieron borrar las canciones.';
				await invalidateAll();
				return;
			}
			player.removeTracks(ids);
			selectedIds = [];
			pendingDelete = [];
			await update({ reset: false });
			hiddenIds = hiddenIds.filter((id) => !ids.includes(id));
		};
	};

	const publishSubmit: SubmitFunction = ({ formData }) => {
		const ids = String(formData.get('trackIds') ?? '').split(',');
		const value = formData.get('isPublished') === 'true';
		overrides = { ...overrides, ...Object.fromEntries(ids.map((id) => [id, value])) };
		return async ({ update }) => {
			await update({ reset: false });
			overrides = {};
			selectedIds = [];
			pendingUnpublish = [];
		};
	};

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

<p class="sr-only" aria-live="polite">{announcement}</p>

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
	use:enhance={publishSubmit}
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
	onClose={() => {
		pendingDelete = [];
		deleteError = '';
	}}
	title="¿Borrar {pendingTitle(pendingDelete)}?"
	description="Esta acción no se puede deshacer: se eliminarán de tu biblioteca, de tus álbumes y de las playlists donde estén."
	confirmLabel="Borrar"
	action="?/deleteTracks"
	submit={deleteSubmit}
	error={deleteError}
	fields={{ trackIds: pendingDelete.map((track) => track.id).join(',') }}
/>

<ConfirmDialog
	open={pendingUnpublish.length > 0}
	onClose={() => (pendingUnpublish = [])}
	title="¿Despublicar {pendingTitle(pendingUnpublish)}?"
	description="Dejarán de verse en álbumes, playlists, búsquedas y perfiles. Solo tú podrás verlas y reproducirlas."
	confirmLabel="Despublicar"
	action="?/setPublished"
	submit={publishSubmit}
	fields={{ trackIds: pendingUnpublish.map((track) => track.id).join(','), isPublished: 'false' }}
/>
