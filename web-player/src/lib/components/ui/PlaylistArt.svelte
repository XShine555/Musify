<script lang="ts">
	import Cover from './Cover.svelte';
	import { gradientForHue, hueFor } from '$lib/theme/color';

	interface Props {
		playlistId?: string;
		trackIds: (string | number)[];
		hue: number;
		size?: 'small' | 'medium' | 'large';
		version?: string;
		class?: string;
	}

	let { playlistId, trackIds, hue, size = 'medium', version, class: klass = '' }: Props = $props();

	let coverFailed = $state(false);

	const mosaic = $derived(trackIds.length >= 4);
	const single = $derived(trackIds.length >= 1 && trackIds.length < 4 ? trackIds[0] : null);
</script>

{#if playlistId && !coverFailed}
	<div class="relative overflow-hidden {klass}" style="background:{gradientForHue(hue)}">
		<img
			src="/api/playlists/{playlistId}/cover?size={size}{version
				? `&v=${encodeURIComponent(version)}`
				: ''}"
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
