<script lang="ts" module>
	import type { Track } from '$lib/types';
	import { COL, type GridColumn } from '$lib/utils/gridColumns';

	export type TrackListColumn = 'added' | 'uploaded' | 'plays';

	export type TrackListTrack = Track & { date?: string | number };

	const COLUMN_PRESET: Record<TrackListColumn, GridColumn> = {
		added: {
			id: 'added',
			label: 'Añadida',
			width: COL.date,
			align: 'center',
			class: 'truncate text-xs text-fg-2 tabular-nums'
		},
		uploaded: {
			id: 'uploaded',
			label: 'Subida',
			width: COL.date,
			align: 'center',
			class: 'truncate text-xs text-fg-2 tabular-nums'
		},
		plays: {
			id: 'plays',
			label: 'Escuchas',
			width: COL.plays,
			align: 'center',
			class: 'truncate text-xs text-fg-2 tabular-nums'
		}
	};
</script>

<script lang="ts">
	import { enhance } from '$app/forms';
	import type { LucideIcon } from '@lucide/svelte';
	import Hash from '@lucide/svelte/icons/hash';
	import Play from '@lucide/svelte/icons/play';
	import { player } from '$lib/player/player.svelte';
	import { fmtTime, fmtDate } from '$lib/utils/format';
	import TrackTitleCell from '$lib/components/music/TrackTitleCell.svelte';
	import DataGrid from '$lib/components/ui/media/DataGrid.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';

	interface RowAction {
		action?: string;
		onclick?: (track: TrackListTrack) => void;
		icon: LucideIcon;
		label: string;
	}

	interface Props {
		tracks: TrackListTrack[];
		columns?: TrackListColumn[];
		index?: boolean;
		onPlay: (index: number) => void;
		rowAction?: RowAction;
		oncontextmenu?: (event: MouseEvent, track: TrackListTrack, index: number) => void;
		class?: string;
	}

	let {
		tracks,
		columns = [],
		index = true,
		onPlay,
		rowAction,
		oncontextmenu,
		class: klass = ''
	}: Props = $props();

	const gridColumns = $derived<GridColumn[]>([
		...(index
			? [
					{
						id: 'index',
						width: COL.index,
						mobile: COL.indexMobile,
						customHead: true
					}
				]
			: []),
		{ id: 'title', label: 'Título', width: COL.title, mobile: COL.titleMobile, mobileLabel: true },
		...columns.map((column) => COLUMN_PRESET[column]),
		{
			id: 'duration',
			label: 'Duración',
			width: COL.durationWide,
			mobile: COL.durationMobile,
			align: 'center',
			class: 'truncate text-xs text-fg-2 tabular-nums'
		},
		...(rowAction ? [{ id: 'action', width: COL.action, mobile: COL.actionMobile }] : [])
	]);
</script>

<DataGrid
	class={klass}
	columns={gridColumns}
	rows={tracks}
	rowKey={(track) => track.id}
	gap="lg"
	rowClass={(track) => (player.currentId === track.id ? 'surface-active' : 'hover:bg-hover')}
	oncontextmenu={oncontextmenu ? (event, track, i) => oncontextmenu(event, track, i) : undefined}
>
	{#snippet head()}
		<Hash class="size-icon-sm" />
	{/snippet}
	{#snippet cell(track, column, i)}
		{@const active = player.currentId === track.id}
		{#if column.id === 'index'}
			<button
				type="button"
				onclick={() => onPlay(i)}
				aria-label={active && player.playing ? 'Pausar' : 'Reproducir'}
				class="relative grid size-8 place-items-center rounded-tag text-xs text-fg-2 tabular-nums"
			>
				{#if active}
					<EqBars paused={!player.playing} />
				{:else}
					<span class="group-hover:hidden">{i + 1}</span>
					<Play class="hidden size-icon-sm text-fg group-hover:block" fill="currentColor" />
				{/if}
			</button>
		{:else if column.id === 'title'}
			<TrackTitleCell {track} {active} eqOverlay={!index} onPlay={() => onPlay(i)} />
		{:else if column.id === 'plays'}
			{track.listensCount}
		{:else if column.id === 'added' || column.id === 'uploaded'}
			{#if track.date}
				{fmtDate(track.date)}
			{/if}
		{:else if column.id === 'duration'}
			{fmtTime(track.duration)}
		{:else if rowAction}
			{#if rowAction.action}
				<form method="POST" action={rowAction.action} use:enhance>
					<input type="hidden" name="trackId" value={track.id} />
					<IconButton type="submit" label={rowAction.label} size="xs" revealOnHover>
						<rowAction.icon class="size-icon-sm" />
					</IconButton>
				</form>
			{:else}
				<IconButton
					label={rowAction.label}
					size="xs"
					revealOnHover
					onclick={() => rowAction.onclick?.(track)}
				>
					<rowAction.icon class="size-icon-sm" />
				</IconButton>
			{/if}
		{/if}
	{/snippet}
</DataGrid>
