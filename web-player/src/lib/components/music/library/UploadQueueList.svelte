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
		{ id: 'actions', width: COL.actions, mobile: COL.actionsMobile }
	];
</script>

<script lang="ts">
	import Check from '@lucide/svelte/icons/check';
	import RotateCcw from '@lucide/svelte/icons/rotate-ccw';
	import X from '@lucide/svelte/icons/x';
	import { fmtTime, formatSize } from '$lib/utils/format';
	import { isBusy, type QueuedUpload } from '$lib/data/uploadQueue';
	import DataGrid from '$lib/components/ui/media/DataGrid.svelte';
	import StatusBadge from '$lib/components/ui/media/StatusBadge.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';

	interface Props {
		items: QueuedUpload[];
		genresLabel: string;
		onRemove: (id: string) => void;
		onCancel: (id: string) => void;
		onRetry: (id: string) => void;
		onTitle: (id: string, title: string) => void;
		class?: string;
	}

	let {
		items,
		genresLabel,
		onRemove,
		onCancel,
		onRetry,
		onTitle,
		class: klass = ''
	}: Props = $props();

	function editable(item: QueuedUpload): boolean {
		return item.phase === 'queued' || item.phase === 'error';
	}

	function details(item: QueuedUpload): string {
		return [
			genresLabel,
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
						disabled={!editable(item)}
						aria-label="Título de {item.file.name}"
						class="w-full truncate rounded-tag bg-transparent text-sm text-fg transition-colors hover:bg-surface focus:bg-surface-2 focus:outline-none disabled:hover:bg-transparent"
					/>
					<p class="truncate text-xs text-fg-2">{item.note ?? item.file.name}</p>
					<p class="truncate text-xs text-fg-2 sm:hidden">{details(item)}</p>
				</div>
			</div>
		{:else if column.id === 'genres'}
			{genresLabel}
		{:else if column.id === 'size'}
			{formatSize(item.file.size) || '-'}
		{:else if column.id === 'duration'}
			{item.duration === null ? '-' : fmtTime(item.duration)}
		{:else if column.id === 'status'}
			{#if item.error}
				<StatusBadge label={item.error} tone="danger" />
			{:else if item.phase === 'uploading'}
				<div
					class="flex items-center gap-2"
					role="progressbar"
					aria-label="Subiendo {item.file.name}"
					aria-valuemin={0}
					aria-valuemax={100}
					aria-valuenow={Math.round(item.progress * 100)}
				>
					<div class="h-1.5 min-w-0 flex-1 overflow-hidden rounded-full bg-btn">
						<div
							class="h-full rounded-full bg-accent transition-[width]"
							style="width:{item.progress * 100}%"
						></div>
					</div>
					<span class="w-9 text-right text-xs text-fg-2 tabular-nums">
						{Math.round(item.progress * 100)}%
					</span>
				</div>
			{:else if item.phase === 'creating'}
				<StatusBadge label="Creando" />
			{:else if item.phase === 'processing'}
				<StatusBadge label="Procesando" tone="neutral" pulse />
			{:else if item.phase === 'done'}
				<span class="inline-flex items-center gap-1.5 text-xs font-medium text-accent-soft">
					<Check class="animate-pop size-icon-sm" />
					Lista
				</span>
			{:else if item.phase === 'error'}
				<StatusBadge label={item.failure ?? 'Error'} tone="danger" />
			{:else}
				<StatusBadge label="En cola" />
			{/if}
		{:else}
			<div class="flex items-center justify-end">
				{#if isBusy(item)}
					<IconButton label="Cancelar subida" size="xs" onclick={() => onCancel(item.id)}>
						<X class="size-icon-sm" />
					</IconButton>
				{:else}
					{#if item.phase === 'error' && !item.error}
						<IconButton label="Reintentar" size="xs" onclick={() => onRetry(item.id)}>
							<RotateCcw class="size-icon-sm" />
						</IconButton>
					{/if}
					{#if item.phase !== 'processing'}
						<IconButton label="Quitar de la cola" size="xs" onclick={() => onRemove(item.id)}>
							<X class="size-icon-sm" />
						</IconButton>
					{/if}
				{/if}
			</div>
		{/if}
	{/snippet}
</DataGrid>
