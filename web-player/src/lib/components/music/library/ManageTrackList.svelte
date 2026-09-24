<script lang="ts" module>
	import { COL, type GridColumn } from '$lib/utils/gridColumns';

	const TEXT = 'truncate text-xs text-fg-2';

	const COLUMNS: GridColumn[] = [
		{ id: 'select', width: COL.check, mobile: COL.check, customHead: true },
		{ id: 'title', label: 'Título', width: COL.title, mobile: COL.titleMobile, mobileLabel: true },
		{ id: 'album', label: 'Álbum', width: COL.album, class: TEXT },
		{ id: 'genres', label: 'Géneros', width: COL.genres, class: TEXT },
		{ id: 'status', label: 'Estado', width: COL.status },
		{
			id: 'duration',
			label: 'Duración',
			width: COL.duration,
			mobile: COL.durationMobile,
			align: 'center',
			class: `${TEXT} tabular-nums`
		},
		{ id: 'publish', width: COL.publish },
		{ id: 'delete', width: COL.action, mobile: COL.actionMobile }
	];
</script>

<script lang="ts">
	import X from '@lucide/svelte/icons/x';
	import type { LibraryTrack } from '$lib/types';
	import { player } from '$lib/player/player.svelte';
	import { genreLabels } from '$lib/data/genres';
	import { canPublish } from '$lib/data/libraryTracks';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import { fmtTime } from '$lib/utils/format';
	import TrackTitleCell from '$lib/components/music/TrackTitleCell.svelte';
	import TrackStatusBadge from '$lib/components/music/TrackStatusBadge.svelte';
	import DataGrid from '$lib/components/ui/media/DataGrid.svelte';
	import Checkbox from '$lib/components/ui/primitives/Checkbox.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';

	interface Props {
		tracks: LibraryTrack[];
		selectedIds: string[];
		onPlay: (index: number) => void;
		onPublish: (track: LibraryTrack) => void;
		onDelete: (track: LibraryTrack) => void;
		class?: string;
	}

	let {
		tracks,
		selectedIds = $bindable(),
		onPlay,
		onPublish,
		onDelete,
		class: klass = ''
	}: Props = $props();

	const selected = $derived(new Set(selectedIds));
	const allSelected = $derived(
		tracks.length > 0 && tracks.every((track) => selected.has(track.id))
	);

	function toggleAll(checked: boolean) {
		selectedIds = checked ? tracks.map((track) => track.id) : [];
	}

	function toggle(id: string, checked: boolean) {
		selectedIds = checked ? [...selectedIds, id] : selectedIds.filter((item) => item !== id);
	}
</script>

<DataGrid
	class={klass}
	columns={COLUMNS}
	rows={tracks}
	rowKey={(track) => track.id}
	stagger
	rowClass={(track) => (player.currentId === track.id ? 'surface-active' : 'hover:bg-hover')}
>
	{#snippet head()}
		<Checkbox label="Seleccionar todas" bind:checked={() => allSelected, toggleAll} />
	{/snippet}
	{#snippet cell(track, column, i)}
		{#if column.id === 'select'}
			<Checkbox
				label="Seleccionar «{track.title}»"
				bind:checked={() => selected.has(track.id), (checked) => toggle(track.id, checked)}
			/>
		{:else if column.id === 'title'}
			<TrackTitleCell {track} active={player.currentId === track.id} onPlay={() => onPlay(i)} />
		{:else if column.id === 'album'}
			{track.albumTitle ?? '-'}
		{:else if column.id === 'genres'}
			{genreLabels(track.genres)}
		{:else if column.id === 'status'}
			<TrackStatusBadge {track} />
		{:else if column.id === 'duration'}
			{fmtTime(track.duration)}
		{:else if column.id === 'publish'}
			<span title={canPublish(track) ? undefined : 'Disponible cuando termine de procesarse'}>
				<Button
					size="sm"
					variant="secondary"
					disabled={!canPublish(track)}
					onclick={() => onPublish(track)}
				>
					{track.published ? 'Despublicar' : 'Publicar'}
				</Button>
			</span>
		{:else}
			<IconButton label="Borrar canción" size="xs" revealOnHover onclick={() => onDelete(track)}>
				<X class="size-icon-sm" />
			</IconButton>
		{/if}
	{/snippet}
</DataGrid>
