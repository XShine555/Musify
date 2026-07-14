<script lang="ts">
	import Cover from './Cover.svelte';
	import { gradientForHue, HUES } from '$lib/theme/color';

	interface Props {
		trackIds: (string | number)[];
		hue: number;
		class?: string;
	}

	let { trackIds, hue, class: klass = '' }: Props = $props();

	function hueFor(id: string | number) {
		const text = String(id);
		let hash = 0;
		for (let i = 0; i < text.length; i++) hash = (hash * 31 + text.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	const mosaic = $derived(trackIds.length >= 4);
	const single = $derived(trackIds.length >= 1 && trackIds.length < 4 ? trackIds[0] : null);
</script>

{#if mosaic}
	<div class="grid grid-cols-2 grid-rows-2 overflow-hidden {klass}">
		{#each trackIds.slice(0, 4) as id (id)}
			<Cover trackId={id} hue={hueFor(id)} size="small" class="h-full w-full" />
		{/each}
	</div>
{:else if single !== null}
	<Cover trackId={single} hue={hueFor(single)} size="medium" class={klass} />
{:else}
	<div class={klass} style="background:{gradientForHue(hue)}"></div>
{/if}
