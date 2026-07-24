<script lang="ts">
	import MediaCard from './MediaCard.svelte';
	import PlaylistArt from './PlaylistArt.svelte';
	import Cover from './Cover.svelte';

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

<MediaCard
	href={href ?? `/albums/${id}`}
	{title}
	subtitle={meta}
	{index}
	oncontextmenu={onContextMenu}
>
	{#snippet art(artClass)}
		{#if coverSrc}
			<Cover trackId={id} src={coverSrc} size="large" alt={title} class={artClass} />
		{:else}
			<PlaylistArt {trackIds} class={artClass} />
		{/if}
	{/snippet}
</MediaCard>
