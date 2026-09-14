<script lang="ts">
	import { getContext } from 'svelte';
	import type { Snippet } from 'svelte';
	import { trackTableKey, type TrackTableContext } from './TrackTable.svelte';

	interface Props {
		active?: boolean;
		class?: string;
		oncontextmenu?: (event: MouseEvent) => void;
		children: Snippet;
	}

	let { active = false, class: klass = '', oncontextmenu, children }: Props = $props();

	const table = getContext<TrackTableContext>(trackTableKey);
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	class="group grid track-grid items-center gap-3 rounded-control py-3 pr-4 pl-2 transition-colors sm:gap-8 sm:pr-6 sm:pl-3 {active
		? 'bg-accent-tint'
		: 'hover:bg-white/[3.2%]'} {klass}"
	style="--mf-track-cols:{table.columns}; --mf-track-cols-mobile:{table.columnsMobile}"
	{oncontextmenu}
>
	{@render children()}
</div>
