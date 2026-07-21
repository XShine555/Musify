<script lang="ts">
	import PlaylistArt from './PlaylistArt.svelte';
	import Cover from './Cover.svelte';
	import { hueFor } from '$lib/theme/color';

	interface Props {
		id: string;
		title: string;
		href?: string;
		subtitle?: string;
		typeLabel?: string;
		releaseYear?: number;
		trackCount?: number;
		trackIds?: (string | number)[];
		coverSrc?: string | null;
		index?: number;
		onContextMenu?: (event: MouseEvent) => void;
	}

	let {
		id,
		title,
		href,
		subtitle,
		typeLabel,
		releaseYear,
		trackCount,
		trackIds = [],
		coverSrc,
		index = 0,
		onContextMenu
	}: Props = $props();

	const meta = $derived(
		[
			typeLabel,
			subtitle,
			releaseYear ? String(releaseYear) : undefined,
			trackCount === undefined
				? undefined
				: `${trackCount} ${trackCount === 1 ? 'canción' : 'canciones'}`
		]
			.filter((part) => !!part)
			.join(' · ')
	);
</script>

<a
	href={href ?? `/albums/${id}`}
	class="group animate-enter min-w-0 text-left"
	style="animation-delay:{index * 45}ms"
	oncontextmenu={onContextMenu}
>
	{#if coverSrc}
		<Cover
			trackId={id}
			src={coverSrc}
			hue={hueFor(id)}
			size="large"
			alt={title}
			class="aspect-square w-full rounded-art shadow-art transition group-hover:shadow-art-lg"
		/>
	{:else}
		<PlaylistArt
			{trackIds}
			hue={hueFor(id)}
			class="aspect-square w-full rounded-art shadow-art transition group-hover:shadow-art-lg"
		/>
	{/if}
	<div class="mt-2.5 truncate text-fg">{title}</div>
	{#if meta}
		<div class="mt-0.5 truncate text-sm text-fg-3">{meta}</div>
	{/if}
</a>
