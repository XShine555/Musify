<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import MixArt from './MixArt.svelte';
	import { player } from '$lib/player/player.svelte';
	import { queueItemForTarget } from '$lib/tracks';
	import { mixItemTarget, type Mix } from '$lib/mixes';

	interface Props {
		mix: Mix;
		index?: number;
		class?: string;
	}

	let { mix, index = 0, class: klass = '' }: Props = $props();

	function play(event: MouseEvent) {
		event.preventDefault();
		const items = mix.items.map((item) => queueItemForTarget(mixItemTarget(item)));
		player.playQueue(items, 0);
	}
</script>

<a
	href="/mixes/{mix.id}"
	class="group/tile animate-enter block cursor-pointer {klass}"
	style="animation-delay:{Math.min(index, 10) * 45}ms"
>
	<div class="relative aspect-square overflow-hidden rounded-art shadow-art">
		<MixArt
			items={mix.items}
			class="h-full w-full scale-100 transition duration-300 group-hover/tile:scale-105"
		/>
		<div
			class="pointer-events-none absolute inset-0 bg-linear-to-b from-transparent from-45% to-black/42"
		></div>
		<button
			type="button"
			onclick={play}
			aria-label="Reproducir {mix.title}"
			class="absolute right-2.5 bottom-2.5 grid h-9.5 w-9.5 place-items-center rounded-full border border-on-art/12 bg-ink/76 text-fg opacity-0 backdrop-blur-[8px] transition-opacity duration-150 group-hover/tile:opacity-100"
		>
			<Play class="h-3.5 w-3.5" fill="currentColor" />
		</button>
	</div>
	<div class="mt-3.25 truncate text-sm font-semibold tracking-[-0.01em] text-fg">
		{mix.title}
	</div>
	{#if mix.subtitle}
		<div class="mt-1 truncate text-xs text-fg-3">{mix.subtitle}</div>
	{/if}
</a>
