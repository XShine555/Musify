<script lang="ts" module>
	const GRID_COLUMNS = [
		'minmax(200px,1fr)',
		'minmax(96px,220px)',
		'96px',
		'88px',
		'72px',
		'140px',
		'36px'
	].join(' ');
	const GRID_COLUMNS_MOBILE = ['minmax(120px,1fr)', '96px', '32px'].join(' ');
	const GRID_STYLE = `--mf-track-cols:${GRID_COLUMNS}; --mf-track-cols-mobile:${GRID_COLUMNS_MOBILE}`;
</script>

<script lang="ts">
	import X from '@lucide/svelte/icons/x';
	import { fmtTime, formatSize } from '$lib/utils/format';
	import type { QueuedUpload } from '$lib/data/uploadQueue';
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
</script>

<div class="overflow-x-auto {klass}">
	<div
		class="grid track-grid items-center gap-3 border-b border-line pr-4 pb-3 pl-2 text-sm text-fg-2 sm:gap-6 sm:pr-6 sm:pl-3"
		style={GRID_STYLE}
	>
		<span>Título</span>
		<span class="hidden sm:block">Géneros</span>
		<span class="hidden sm:block">Acceso</span>
		<span class="hidden sm:block">Tamaño</span>
		<span class="hidden text-center sm:block">Duración</span>
		<span>Estado</span>
		<span></span>
	</div>
	<div class="mt-1 flex flex-col gap-0.5" role="list">
		{#each items as item (item.id)}
			<div
				role="listitem"
				class="group grid track-grid items-center gap-3 rounded-control py-3 pr-4 pl-2 transition-colors hover:bg-hover sm:gap-6 sm:pr-6 sm:pl-3"
				style={GRID_STYLE}
			>
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
					</div>
				</div>
				<span class="hidden min-w-0 truncate text-xs text-fg-2 sm:block">{genresLabel}</span>
				<span class="hidden min-w-0 truncate text-xs text-fg-2 sm:block">{visibilityLabel}</span>
				<span class="hidden truncate text-xs text-fg-2 tabular-nums sm:block">
					{formatSize(item.file.size) || '-'}
				</span>
				<span class="hidden truncate text-center text-xs text-fg-2 tabular-nums sm:block">
					{item.duration === null ? '-' : fmtTime(item.duration)}
				</span>
				<span>
					{#if item.error}
						<StatusBadge label={item.error} tone="danger" />
					{:else}
						<StatusBadge label="Lista" tone="success" />
					{/if}
				</span>
				<IconButton label="Quitar de la cola" size="xs" onclick={() => onRemove(item.id)}>
					<X class="size-icon-sm" />
				</IconButton>
			</div>
		{/each}
	</div>
</div>
