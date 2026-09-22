<script lang="ts">
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';
	import { previousPage } from '$lib/state/navigation.svelte';

	interface Props {
		href: string;
		label: string;
	}

	let { href, label }: Props = $props();

	const previous = $derived(previousPage());
	const target = $derived(previous ?? { url: href, label });

	function onclick(event: MouseEvent) {
		if (!previous) return;
		if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey)
			return;
		event.preventDefault();
		history.back();
	}
</script>

<a
	href={target.url}
	{onclick}
	class="inline-flex items-center text-fg-3 transition-colors hover:text-accent-soft"
>
	<span class="grid size-8 place-items-center">
		<ArrowLeft class="size-icon-sm" />
	</span>
	{target.label}
</a>
