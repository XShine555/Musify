<script lang="ts">
	import type { Snippet } from 'svelte';

	type Size = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl' | 'hero' | 'fill';

	interface Props {
		src?: string | null;
		trackIds?: (string | number)[];
		size: Size;
		shape?: 'auto' | 'round';
		imageSize?: 'small' | 'medium' | 'large';
		fallback?: 'music' | 'gradient' | 'none';
		alt?: string;
		class?: string;
		children?: Snippet;
	}

	let {
		src,
		trackIds = [],
		size,
		shape = 'auto',
		imageSize,
		fallback = 'music',
		alt = '',
		class: klass = '',
		children
	}: Props = $props();

	const DIMENSION: Record<Size, string> = {
		xs: 'size-cover-xs',
		sm: 'size-cover-sm',
		md: 'size-cover-md',
		lg: 'size-cover-lg',
		xl: 'size-cover-xl',
		'2xl': 'size-cover-2xl',
		hero: 'size-cover-hero sm:size-cover-hero-sm',
		fill: 'aspect-square w-full'
	};

	const RADIUS: Record<Size, string> = {
		xs: 'rounded-thumb',
		sm: 'rounded-art',
		md: 'rounded-art',
		lg: 'rounded-art',
		xl: 'rounded-art',
		'2xl': 'rounded-art-lg',
		hero: 'rounded-art-lg',
		fill: ''
	};

	const AUTO_IMAGE_SIZE: Record<Size, 'small' | 'medium' | 'large'> = {
		xs: 'small',
		sm: 'small',
		md: 'medium',
		lg: 'medium',
		xl: 'medium',
		'2xl': 'large',
		hero: 'large',
		fill: 'large'
	};

	const resolvedImageSize = $derived(imageSize ?? AUTO_IMAGE_SIZE[size]);
	const radius = $derived(shape === 'round' ? 'rounded-full' : RADIUS[size]);

	let srcFailed = $state(false);
	$effect(() => {
		src;
		srcFailed = false;
	});

	let singleFailed = $state(false);
	$effect(() => {
		trackIds[0];
		singleFailed = false;
	});

	const showSrc = $derived(!!src && !srcFailed);
	const showMosaic = $derived(!showSrc && trackIds.length >= 4);
	const singleId = $derived(!showSrc && !showMosaic && trackIds.length > 0 ? trackIds[0] : null);
	const showSingle = $derived(singleId !== null && !singleFailed);
</script>

<div class="relative overflow-hidden {DIMENSION[size]} {radius} {klass}">
	{#if showSrc}
		<img
			{src}
			{alt}
			loading="lazy"
			decoding="async"
			fetchpriority="low"
			onerror={() => (srcFailed = true)}
			class="h-full w-full object-cover"
		/>
	{:else if showMosaic}
		<div class="grid h-full w-full grid-cols-2 grid-rows-2">
			{#each trackIds.slice(0, 4) as id (id)}
				<img
					src="/api/tracks/{id}/cover?size=small"
					alt=""
					loading="lazy"
					decoding="async"
					fetchpriority="low"
					onerror={(e) => ((e.currentTarget as HTMLImageElement).style.visibility = 'hidden')}
					class="h-full w-full object-cover"
				/>
			{/each}
		</div>
	{:else if showSingle}
		<img
			src="/api/tracks/{singleId}/cover?size={resolvedImageSize}"
			{alt}
			loading="lazy"
			decoding="async"
			fetchpriority="low"
			onerror={() => (singleFailed = true)}
			class="h-full w-full object-cover"
		/>
	{:else if fallback === 'music'}
		<div class="grid h-full w-full place-items-center bg-surface">
			<svg viewBox="0 0 24 24" fill="currentColor" class="h-2/5 w-2/5 text-fg-2">
				<path
					d="M19.952 1.651a.75.75 0 0 1 .298.599V16.303a3 3 0 0 1-2.176 2.884l-1.32.377a2.553 2.553 0 1 1-1.401-4.909l2.311-.66a1.5 1.5 0 0 0 1.088-1.442V6.994l-9 2.572v9.737a3 3 0 0 1-2.176 2.884l-1.32.377a2.553 2.553 0 1 1-1.402-4.909l2.312-.66a1.5 1.5 0 0 0 1.088-1.442V5.25a.75.75 0 0 1 .544-.721l10.5-3a.75.75 0 0 1 .754.122Z"
				/>
			</svg>
		</div>
	{:else if fallback === 'gradient'}
		<div class="h-full w-full bg-(image:--mf-cover-grad)"></div>
	{:else}
		<div class="h-full w-full bg-surface"></div>
	{/if}
	{@render children?.()}
</div>
