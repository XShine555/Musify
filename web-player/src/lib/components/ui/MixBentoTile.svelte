<script lang="ts">
	import MixArt from './MixArt.svelte';
	import { hueFor } from '$lib/theme/color';
	import type { Mix } from '$lib/mixes';

	interface Props {
		mix: Mix;
		featured?: boolean;
		banner?: boolean;
		class?: string;
		index?: number;
	}

	let { mix, featured = false, banner = false, class: klass = '', index = 0 }: Props = $props();
</script>

<a
	href="/mixes/{mix.id}"
	class="group/tile animate-enter relative block overflow-hidden rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset hover:shadow-art-lg focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none {klass}"
	style="animation-delay:{Math.min(index, 10) * 45}ms"
>
	<MixArt
		items={mix.items}
		hue={hueFor(mix.id)}
		class="absolute inset-0 h-full w-full transition duration-500 ease-out group-hover/tile:scale-[1.04]"
	/>
	<div
		class="pointer-events-none absolute inset-0"
		style="background:linear-gradient(to top, rgba(0,0,0,0.78) 0%, rgba(0,0,0,0.32) 42%, rgba(0,0,0,0.05) 100%)"
	></div>
	<div
		class="relative flex h-full flex-col justify-end p-3 sm:p-4 {banner
			? 'sm:flex-row sm:items-center sm:justify-between sm:gap-4'
			: ''}"
	>
		<div class="min-w-0">
			<div
				class="truncate font-semibold text-white {featured ? 'text-xl sm:text-2xl' : 'text-base'}"
			>
				{mix.title}
			</div>
			{#if mix.subtitle}
				<div class="mt-0.5 truncate text-xs text-white/70 sm:text-sm">{mix.subtitle}</div>
			{/if}
		</div>
	</div>
</a>
