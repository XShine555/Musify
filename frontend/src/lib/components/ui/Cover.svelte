<script lang="ts">
	import type { Snippet } from 'svelte';
	import { gradientForHue } from '$lib/theme/color';

	interface Props {
		trackId: string | number;
		hue: number;
		size?: 'small' | 'medium' | 'large';
		src?: string;
		alt?: string;
		class?: string;
		children?: Snippet;
	}

	let { trackId, hue, size = 'medium', src, alt = '', class: klass = '', children }: Props = $props();

	let failed = $state(false);
	let loaded = $state(false);

	const imageSrc = $derived(src ?? `/api/tracks/${trackId}/cover?size=${size}`);

	$effect(() => {
		imageSrc;
		failed = false;
		loaded = false;
	});
</script>

<div
	class="relative overflow-hidden {klass}"
	style={loaded && !failed ? undefined : `background:${gradientForHue(hue)}`}
>
	{#if !failed}
		<img
			src={imageSrc}
			{alt}
			loading="lazy"
			onload={() => (loaded = true)}
			onerror={() => (failed = true)}
			class="h-full w-full object-cover"
		/>
	{/if}
	{@render children?.()}
</div>
