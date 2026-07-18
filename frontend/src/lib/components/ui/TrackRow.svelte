<script lang="ts">
	import { getContext } from 'svelte';
	import type { Snippet } from 'svelte';
	import { trackTableKey, type TrackTableContext } from './TrackTable.svelte';

	interface Props {
		class?: string;
		oncontextmenu?: (event: MouseEvent) => void;
		children: Snippet;
	}

	let { class: klass = '', oncontextmenu, children }: Props = $props();

	const table = getContext<TrackTableContext>(trackTableKey);
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	class="group grid items-center gap-8 rounded-control px-3 py-2.5 transition hover:bg-surface {klass}"
	style="grid-template-columns:{table.columns}"
	{oncontextmenu}
>
	{@render children()}
</div>
