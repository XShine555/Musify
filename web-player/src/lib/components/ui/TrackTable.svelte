<script lang="ts" module>
	export const trackTableKey = Symbol('track-table');

	export interface TrackTableContext {
		readonly columns: string;
		readonly columnsMobile: string;
	}

	export interface TrackMetaColumn {
		label: string;
		width: string;
	}
</script>

<script lang="ts">
	import { setContext } from 'svelte';
	import type { Snippet } from 'svelte';

	interface Props {
		index?: boolean;
		action?: boolean;
		meta?: TrackMetaColumn[];
		class?: string;
		children: Snippet;
	}

	let { index = false, action = false, meta = [], class: klass = '', children }: Props = $props();

	const columns = $derived(
		[
			index ? '32px' : '',
			'1fr',
			...meta.map((column) => column.width),
			'64px',
			action ? '36px' : ''
		]
			.filter((width) => width !== '')
			.join(' ')
	);

	const columnsMobile = $derived(
		[index ? '28px' : '', '1fr', '52px', action ? '32px' : '']
			.filter((width) => width !== '')
			.join(' ')
	);

	setContext<TrackTableContext>(trackTableKey, {
		get columns() {
			return columns;
		},
		get columnsMobile() {
			return columnsMobile;
		}
	});
</script>

<div class={klass}>
	<div
		class="grid track-grid items-center gap-3 border-b border-line pr-4 pb-2 pl-2 text-sm font-medium tracking-wide text-muted uppercase sm:gap-8 sm:pr-6 sm:pl-3"
		style="--mf-track-cols:{columns}; --mf-track-cols-mobile:{columnsMobile}"
	>
		{#if index}
			<span class="text-center">#</span>
		{/if}
		<span>Título</span>
		{#each meta as column (column.label)}
			<span class="hidden text-center sm:block">{column.label}</span>
		{/each}
		<span class="hidden text-center sm:block">Duración</span>
		{#if action}
			<span></span>
		{/if}
	</div>
	<div class="mt-1 flex flex-col">
		{@render children()}
	</div>
</div>
