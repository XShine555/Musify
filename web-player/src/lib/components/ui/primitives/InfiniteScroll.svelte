<script lang="ts">
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';
	import Button from './Button.svelte';
	import { INFINITE_SCROLL_ROOT_MARGIN } from '$lib/config';

	interface Props {
		onLoadMore: () => void;
		hasMore: boolean;
		loading: boolean;
		error?: boolean;
		rootMargin?: string;
	}

	let {
		onLoadMore,
		hasMore,
		loading,
		error = false,
		rootMargin = INFINITE_SCROLL_ROOT_MARGIN
	}: Props = $props();

	let sentinel = $state<HTMLDivElement>();
	let visible = $state(false);

	$effect(() => {
		const node = sentinel;
		if (!node) return;
		const observer = new IntersectionObserver(
			(entries) => {
				visible = entries[0].isIntersecting;
			},
			{ rootMargin }
		);
		observer.observe(node);
		return () => observer.disconnect();
	});

	$effect(() => {
		if (visible && hasMore && !loading && !error) onLoadMore();
	});
</script>

<div bind:this={sentinel} class="flex min-h-12 items-center justify-center py-8">
	{#if loading}
		<LoaderCircle class="size-5 animate-spin text-fg-2" />
	{:else if error}
		<div class="flex items-center gap-3 text-sm text-fg-2">
			No se pudo cargar más.
			<Button variant="secondary" size="sm" onclick={onLoadMore}>Reintentar</Button>
		</div>
	{/if}
</div>
