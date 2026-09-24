<script lang="ts" module>
	import { COL, type GridColumn } from '$lib/utils/gridColumns';

	const TEXT = 'truncate text-xs text-fg-2';

	const COLUMNS: GridColumn[] = [
		{
			id: 'title',
			label: 'Título',
			width: 'minmax(12.5rem,1fr)',
			mobile: COL.titleMobile,
			mobileLabel: true
		},
		{ id: 'genres', label: 'Géneros', width: COL.genresWide, class: TEXT },
		{ id: 'access', label: 'Acceso', width: COL.access, class: TEXT },
		{ id: 'size', label: 'Tamaño', width: COL.size, class: `${TEXT} tabular-nums` },
		{
			id: 'duration',
			label: 'Duración',
			width: COL.durationQueue,
			align: 'center',
			class: `${TEXT} tabular-nums`
		},
		{
			id: 'status',
			label: 'Estado',
			width: COL.statusQueue,
			mobile: COL.statusMobile,
			mobileLabel: true
		},
		{ id: 'remove', width: COL.action, mobile: COL.actionMobile }
	];
</script>

<script lang="ts">
	import X from '@lucide/svelte/icons/x';
	import { fmtTime, formatSize } from '$lib/utils/format';
	import type { QueuedUpload } from '$lib/data/uploadQueue';
	import DataGrid from '$lib/components/ui/media/DataGrid.svelte';
	import StatusBadge from '$lib/components/ui/media/StatusBadge.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';

	interface Props {
		items: QueuedUpload[];
		genresLabel: string;
		visibilityLabel: string;
		onRemove: (id: string) => void;
		onTitle: (id: string, title: string) => void;
		class?: string;
	}

	let {
		items,
		genresLabel,
		visibilityLabel,
		onRemove,
		onTitle,
		class: klass = ''
	}: Props = $props();

	function details(item: QueuedUpload): string {
		return [
			genresLabel,
			visibilityLabel,
			formatSize(item.file.size),
			item.duration === null ? '' : fmtTime(item.duration)
		]
			.filter((part) => part !== '' && part !== '-')
			.join(' · ');
	}
</script>

<DataGrid
	class={klass}
	columns={COLUMNS}
	rows={items}
	rowKey={(item) => item.id}
	rowClass={() => 'hover:bg-hover'}
>
	{#snippet cell(item, column)}
		{#if column.id === 'title'}
			<div class="flex min-w-0 items-center gap-3">
				<span
					class="grid size-10 shrink-0 place-items-center rounded-control bg-surface text-xs font-medium text-fg-2"
				>
					{item.format}
				</span>
				<div class="min-w-0">
					<input
						value={item.title}
						oninput={(event) => onTitle(item.id, event.currentTarget.value)}
						aria-label="Título de {item.file.name}"
						class="w-full truncate rounded-tag bg-transparent text-sm text-fg transition-colors hover:bg-surface focus:bg-surface-2 focus:outline-none"
					/>
					<p class="truncate text-xs text-fg-2">{item.file.name}</p>
					<p class="truncate text-xs text-fg-2 sm:hidden">{details(item)}</p>
				</div>
			</div>
		{:else if column.id === 'genres'}
			{genresLabel}
		{:else if column.id === 'access'}
			{visibilityLabel}
		{:else if column.id === 'size'}
			{formatSize(item.file.size) || '-'}
		{:else if column.id === 'duration'}
			{item.duration === null ? '-' : fmtTime(item.duration)}
		{:else if column.id === 'status'}
			{#if item.error}
				<StatusBadge label={item.error} tone="danger" />
			{:else}
				<StatusBadge label="Lista" tone="success" />
			{/if}
		{:else}
			<IconButton label="Quitar de la cola" size="xs" onclick={() => onRemove(item.id)}>
				<X class="size-icon-sm" />
			</IconButton>
		{/if}
	{/snippet}
</DataGrid>
