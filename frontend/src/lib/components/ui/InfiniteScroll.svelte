<script lang="ts">
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';

	interface Props {
		onLoadMore: () => void;
		hasMore: boolean;
		loading: boolean;
		rootMargin?: string;
	}

	let { onLoadMore, hasMore, loading, rootMargin = '600px' }: Props = $props();

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
		if (visible && hasMore && !loading) onLoadMore();
	});
</script>

<div bind:this={sentinel} class="flex min-h-12 items-center justify-center py-8">
	{#if loading}
		<LoaderCircle class="h-5 w-5 animate-spin text-muted" />
	{/if}
</div>
