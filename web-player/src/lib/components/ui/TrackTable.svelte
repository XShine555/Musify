<script lang="ts" module>
	export const trackTableKey = Symbol('track-table');

	export interface TrackTableContext {
		readonly columns: string;
		readonly columnsMobile: string;
	}
</script>

<script lang="ts">
	import { setContext } from 'svelte';
	import type { Snippet } from 'svelte';

	interface Props {
		columns: string;
		columnsMobile?: string;
		class?: string;
		headers: Snippet;
		children: Snippet;
	}

	let { columns, columnsMobile, class: klass = '', headers, children }: Props = $props();

	const mobile = $derived(columnsMobile ?? columns);

	setContext<TrackTableContext>(trackTableKey, {
		get columns() {
			return columns;
		},
		get columnsMobile() {
			return mobile;
		}
	});
</script>

<div class={klass}>
	<div
		class="grid track-grid items-center gap-3 border-b border-line px-2 pb-2 text-sm font-medium tracking-wide text-muted uppercase sm:gap-8 sm:px-3"
		style="--mf-track-cols:{columns}; --mf-track-cols-mobile:{mobile}"
	>
		{@render headers()}
	</div>
	<div class="mt-1 flex flex-col">
		{@render children()}
	</div>
</div>
