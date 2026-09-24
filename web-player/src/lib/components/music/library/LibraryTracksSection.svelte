<script lang="ts">
	import Upload from '@lucide/svelte/icons/upload';
	import Music from '@lucide/svelte/icons/music';
	import SearchX from '@lucide/svelte/icons/search-x';
	import type { LibraryTrack } from '$lib/types';
	import { player } from '$lib/player/player.svelte';
	import { filterLibraryTracks, TRACK_FILTERS, type TrackFilter } from '$lib/data/libraryTracks';
	import { plural } from '$lib/utils/format';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import SelectionBar from '$lib/components/ui/primitives/SelectionBar.svelte';
	import SearchInput from '$lib/components/ui/primitives/SearchInput.svelte';
	import ChipGroup from '$lib/components/ui/primitives/ChipGroup.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import ManageTrackList from '$lib/components/music/library/ManageTrackList.svelte';

	interface Props {
		tracks: LibraryTrack[];
		errorMessage?: string;
	}

	let { tracks, errorMessage }: Props = $props();

	let query = $state('');
	let filter = $state<TrackFilter>('all');
	let selectedIds = $state<string[]>([]);
	let pendingDelete = $state<LibraryTrack[]>([]);

	const visible = $derived(filterLibraryTracks(tracks, query, filter));
	const filterOptions = $derived(
		TRACK_FILTERS.map(({ value, label }) => ({
			value,
			label,
			count: filterLibraryTracks(tracks, '', value).length
		}))
	);

	const pendingTitle = $derived(
		pendingDelete.length === 1
			? `«${pendingDelete[0].title}»`
			: plural(pendingDelete.length, 'canción', 'canciones')
	);

	function togglePlay(index: number) {
		player.playOrToggle(visible, index);
	}

	$effect(() => {
		const visibleIds = new Set(visible.map((track) => track.id));
		if (selectedIds.some((id) => !visibleIds.has(id))) {
			selectedIds = selectedIds.filter((id) => visibleIds.has(id));
		}
	});

	function deleteSelected() {
		const ids = new Set(selectedIds);
		pendingDelete = visible.filter((track) => ids.has(track.id));
	}
</script>

{#if errorMessage}
	<Alert tone="danger" class="mt-4">{errorMessage}</Alert>
{/if}

{#if tracks.length > 0}
	{#if selectedIds.length > 0}
		<SelectionBar
			class="mt-4"
			label={plural(selectedIds.length, 'seleccionada', 'seleccionadas')}
			onClear={() => (selectedIds = [])}
		>
			<Button size="sm" variant="danger" onclick={deleteSelected}>Borrar</Button>
		</SelectionBar>
	{:else}
		<div class="mt-4 flex min-h-11 flex-wrap items-center gap-x-4 gap-y-3">
			<SearchInput
				bind:value={query}
				placeholder="Filtrar por título o artista"
				class="w-full sm:max-w-search sm:flex-1"
			/>
			<ChipGroup label="Filtrar por estado" options={filterOptions} bind:value={filter} />
		</div>
	{/if}

	{#if visible.length > 0}
		<ManageTrackList
			class="mt-4"
			tracks={visible}
			bind:selectedIds
			onPlay={togglePlay}
			onDelete={(track) => (pendingDelete = [track])}
		/>
	{:else}
		<EmptyState
			icon={SearchX}
			title="Sin resultados"
			description="Ninguna canción coincide con la búsqueda o el filtro."
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
	title="¿Borrar {pendingTitle}?"
	description="Esta acción no se puede deshacer: se eliminarán de tu biblioteca, de tus álbumes y de las playlists donde estén."
	confirmLabel="Borrar"
	action="?/deleteTracks"
	fields={{ trackIds: pendingDelete.map((track) => track.id).join(',') }}
/>
