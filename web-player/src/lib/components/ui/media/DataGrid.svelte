<script lang="ts" module>
	const GAP = { md: 'sm:gap-6', lg: 'sm:gap-8' };
</script>

<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import { flip } from 'svelte/animate';
	import { expoOut } from 'svelte/easing';
	import { collapse } from '$lib/utils/transitions';
	import { gridColumns, type GridColumn } from '$lib/utils/gridColumns';

	interface Props {
		columns: readonly GridColumn[];
		rows: readonly T[];
		rowKey: (row: T) => string;
		cell: Snippet<[T, GridColumn, number]>;
		head?: Snippet<[GridColumn]>;
		rowClass?: (row: T, index: number) => string;
		oncontextmenu?: (event: MouseEvent, row: T, index: number) => void;
		gap?: 'md' | 'lg';
		stagger?: boolean;
		class?: string;
	}

	let {
		columns,
		rows,
		rowKey,
		cell,
		head,
		rowClass,
		oncontextmenu,
		gap = 'md',
		stagger = false,
		class: klass = ''
	}: Props = $props();

	const style = $derived(gridColumns(columns));

	function cellClass(column: GridColumn): string {
		return [column.mobile ? '' : 'hidden sm:block', column.align === 'center' ? 'text-center' : '']
			.filter(Boolean)
			.join(' ');
	}
</script>

<div class="overflow-x-auto {klass}">
	<div
		class="grid track-grid items-center gap-3 border-b border-line pr-4 pb-3 pl-2 text-sm text-fg-2 sm:pr-6 sm:pl-3 {GAP[
			gap
		]}"
		{style}
	>
		{#each columns as column (column.id)}
			<div class="min-w-0 {cellClass(column)}">
				{#if head && column.customHead}
					{@render head(column)}
				{:else if column.label}
					<span class={column.mobileLabel ? '' : 'hidden sm:inline'}>{column.label}</span>
				{/if}
			</div>
		{/each}
	</div>
	<div class="mt-1 flex flex-col gap-0.5" role="list">
		{#each rows as row, i (rowKey(row))}
			<div
				role="listitem"
				animate:flip={{ duration: 200, easing: expoOut }}
				out:collapse
				class="group grid track-grid items-center gap-3 rounded-control py-3 pr-4 pl-2 transition-colors sm:pr-6 sm:pl-3 {GAP[
					gap
				]} {stagger ? 'animate-enter' : ''} {rowClass?.(row, i) ?? ''}"
				style="{style}{stagger ? `; --i:${i}` : ''}"
				oncontextmenu={oncontextmenu ? (event) => oncontextmenu(event, row, i) : undefined}
			>
				{#each columns as column (column.id)}
					<div class="min-w-0 {cellClass(column)} {column.class ?? ''}">
						{@render cell(row, column, i)}
					</div>
				{/each}
			</div>
		{/each}
	</div>
</div>
