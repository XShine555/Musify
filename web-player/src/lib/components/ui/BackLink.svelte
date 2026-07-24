<script lang="ts">
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';
	import { previousPage } from '$lib/navigation.svelte';

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
	class="group inline-flex items-center text-fg-3 transition-colors hover:text-accent-soft"
>
	<span class="grid h-8 w-8 place-items-center transition-colors group-hover:text-accent-soft">
		<ArrowLeft class="h-4 w-4" />
	</span>
	{target.label}
</a>
