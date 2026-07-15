<script lang="ts">
	import Cover from './Cover.svelte';
	import { gradientForHue, HUES } from '$lib/theme/color';

	interface Props {
		playlistId?: string;
		trackIds: (string | number)[];
		hue: number;
		size?: 'small' | 'medium' | 'large';
		class?: string;
	}

	let { playlistId, trackIds, hue, size = 'medium', class: klass = '' }: Props = $props();

	function hueFor(id: string | number) {
		const text = String(id);
		let hash = 0;
		for (let i = 0; i < text.length; i++) hash = (hash * 31 + text.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	let coverFailed = $state(false);

	const mosaic = $derived(trackIds.length >= 4);
	const single = $derived(trackIds.length >= 1 && trackIds.length < 4 ? trackIds[0] : null);
</script>

{#if playlistId && !coverFailed}
	<div class="relative overflow-hidden {klass}" style="background:{gradientForHue(hue)}">
		<img
			src="/api/playlists/{playlistId}/cover?size={size}"
			alt=""
			loading="lazy"
			onerror={() => (coverFailed = true)}
			class="h-full w-full object-cover"
		/>
	</div>
{:else if mosaic}
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
