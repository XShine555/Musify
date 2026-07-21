<script lang="ts">
	import type { Snippet } from 'svelte';
	import { gradientForHue } from '$lib/theme/color';
	import { thumbnailSrc } from '$lib/thumbnails';

	interface Props {
		trackId: string | number;
		hue: number;
		size?: 'small' | 'medium' | 'large';
		src?: string | null;
		alt?: string;
		class?: string;
		children?: Snippet;
		oncontextmenu?: (event: MouseEvent) => void;
	}

	let {
		trackId,
		hue,
		size = 'medium',
		src,
		alt = '',
		class: klass = '',
		children,
		oncontextmenu
	}: Props = $props();

	let failed = $state(false);
	let loaded = $state(false);

	const imageSrc = $derived(
		src ? (thumbnailSrc(src, size) ?? src) : `/api/tracks/${trackId}/cover?size=${size}`
	);

	$effect(() => {
		imageSrc;
		failed = false;
		loaded = false;
	});
</script>

<div
	class="relative overflow-hidden {klass}"
	style={loaded && !failed ? undefined : `background:${gradientForHue(hue)}`}
	role="presentation"
	{oncontextmenu}
>
	{#if !failed}
		<img
			src={imageSrc}
			{alt}
			loading="lazy"
			decoding="async"
			fetchpriority="low"
			onload={() => (loaded = true)}
			onerror={() => (failed = true)}
			class="h-full w-full object-cover"
		/>
	{/if}
	{@render children?.()}
</div>
