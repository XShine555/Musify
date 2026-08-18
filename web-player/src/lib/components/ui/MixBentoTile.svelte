<script lang="ts">
	import Cover from './Cover.svelte';
	import type { Mix } from '$lib/mixes';

	interface Props {
		mix: Mix;
		featured?: boolean;
		banner?: boolean;
		class?: string;
		index?: number;
	}

	let { mix, featured = false, banner = false, class: klass = '', index = 0 }: Props = $props();

	const fillCount = $derived(banner ? 40 : 12);

	const tiles = $derived.by(() => {
		if (mix.items.length === 0) return [];
		const out: Mix['items'] = [];
		while (out.length < fillCount) out.push(...mix.items);
		return out.slice(0, fillCount);
	});
</script>

<a
	href="/mixes/{mix.id}"
	class="group/tile animate-enter relative block overflow-hidden rounded-art ring-1 ring-line transition duration-300 ease-out [clip-path:inset(0_round_var(--radius-art))] ring-inset focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none {klass}"
	style="animation-delay:{Math.min(index, 10) * 45}ms"
>
	{#if tiles.length > 0}
		<div class="absolute inset-0 flex transition duration-300 ease-out">
			{#each tiles as item, i (i)}
				<Cover
					trackId={item.trackId ?? item.videoId ?? ''}
					src={item.thumbnailUrl}
					size="medium"
					class="aspect-square h-full shrink-0"
				/>
			{/each}
		</div>
	{/if}
	<div
		class="pointer-events-none absolute inset-0 bg-linear-to-t from-scrim/85 via-scrim/65 to-scrim/40 {banner
			? 'sm:bg-linear-to-r'
			: ''}"
	></div>
	<div
		class="pointer-events-none absolute inset-0 bg-scrim/25 opacity-0 transition-opacity duration-300"
	></div>
	<div
		class="relative flex h-full flex-col justify-end p-3 sm:p-4 {banner
			? 'sm:flex-row sm:items-center sm:justify-between sm:gap-4'
			: ''}"
	>
		<div class="min-w-0">
			<div class="truncate text-on-art {featured ? 'text-xl sm:text-2xl' : 'text-base'}">
				{mix.title}
			</div>
			{#if mix.subtitle}
				<div class="mt-0.5 truncate text-xs text-on-art/75 sm:text-sm">{mix.subtitle}</div>
			{/if}
		</div>
	</div>
</a>
