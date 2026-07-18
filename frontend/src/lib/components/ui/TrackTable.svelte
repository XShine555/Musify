<script lang="ts" module>
	export const trackTableKey = Symbol('track-table');

	export interface TrackTableContext {
		readonly columns: string;
	}
</script>

<script lang="ts">
	import { setContext } from 'svelte';
	import type { Snippet } from 'svelte';

	interface Props {
		columns: string;
		class?: string;
		headers: Snippet;
		children: Snippet;
	}

	let { columns, class: klass = '', headers, children }: Props = $props();

	setContext<TrackTableContext>(trackTableKey, {
		get columns() {
			return columns;
		}
	});
</script>

<div class={klass}>
	<div
		class="grid items-center gap-8 border-b border-line px-3 pb-2 text-sm font-medium tracking-wide text-muted uppercase"
		style="grid-template-columns:{columns}"
	>
		{@render headers()}
	</div>
	<div class="mt-1 flex flex-col">
		{@render children()}
	</div>
</div>
