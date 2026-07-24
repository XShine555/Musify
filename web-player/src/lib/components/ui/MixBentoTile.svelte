<script lang="ts">
	import Cover from './Cover.svelte';
	import { gradientForHue, hueFor } from '$lib/theme/color';
	import { mixItemKey, type Mix } from '$lib/mixes';

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
	class="group/tile animate-enter relative block overflow-hidden rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset hover:shadow-art-lg focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none {klass}"
	style="animation-delay:{Math.min(index, 10) * 45}ms;background:{gradientForHue(hueFor(mix.id))}"
>
{#if tiles.length > 0}
	<div
		class="absolute inset-0 flex transition duration-300 ease-out group-hover/tile:scale-[1.01]"
	>
		{#each tiles as item, i (i)}
			<Cover
				trackId={item.trackId ?? item.videoId ?? ''}
				src={item.thumbnailUrl}
				hue={hueFor(mixItemKey(item))}
				size="medium"
				class="aspect-square h-full shrink-0"
			/>
		{/each}
	</div>
{/if}
<div
	class="pointer-events-none absolute inset-0 bg-black/10 transition-colors duration-300 group-hover/tile:bg-black/20"
></div>
	<div
		class="group relative flex h-full flex-col justify-end p-3 sm:p-4 {banner
			? 'sm:flex-row sm:items-center sm:justify-between sm:gap-4'
			: ''}"
	>
		<div class="min-w-0">
			<div
				class="truncate text-white {featured ? 'text-xl sm:text-2xl' : 'text-base'} group-hover:text-accent-soft transition-colors"
			>
				{mix.title}
			</div>
			{#if mix.subtitle}
				<div class="mt-0.5 truncate text-xs text-muted sm:text-sm">{mix.subtitle}</div>
			{/if}
		</div>
	</div>
</a>
