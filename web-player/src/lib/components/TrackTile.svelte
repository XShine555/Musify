<script lang="ts">
	import type { Snippet } from 'svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlayPauseOverlay from '$lib/components/ui/PlayPauseOverlay.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';

	interface Props {
		id: string | number;
		title: string;
		artist?: string | null;
		typeLabel?: string;
		coverSrc?: string | null;
		coverSize?: 'small' | 'medium' | 'large';
		coverShape?: 'square' | 'round';
		hue: number;
		explicit?: boolean;
		active: boolean;
		playing: boolean;
		index: number;
		onClick: () => void;
		onContextMenu?: (event: MouseEvent) => void;
		badge?: Snippet;
	}

	let {
		id,
		title,
		artist,
		typeLabel,
		coverSrc,
		coverSize = 'large',
		coverShape = 'square',
		hue,
		explicit = false,
		active,
		playing,
		index,
		onClick,
		onContextMenu,
		badge
	}: Props = $props();

	const meta = $derived([typeLabel, artist].filter((part) => !!part).join(' · '));
</script>

<li
	class="group animate-enter relative min-w-0"
	style="animation-delay:{index * 40}ms"
	oncontextmenu={onContextMenu}
>
	<button
		type="button"
		onclick={onClick}
		aria-label="{active && playing ? 'Pausar' : 'Reproducir'} {title}"
		class="block w-full text-left"
	>
		<Cover
			trackId={id}
			src={coverSrc}
			{hue}
			size={coverSize}
			alt={title}
			class="aspect-square w-full shadow-art {coverShape === 'round'
				? 'rounded-art-round'
				: 'rounded-art'}"
		>
			<PlayPauseOverlay {active} {playing} />
		</Cover>
		<div class="mt-2.5 flex min-w-0 items-center gap-1.5">
			{#if explicit}
				<ExplicitBadge />
			{/if}
			<span class="truncate text-fg" {title}>{title}</span>
		</div>
		{#if badge || meta}
			<div class="mt-0.5 flex min-w-0 items-center gap-1.5">
				{@render badge?.()}
				{#if meta}
					<span class="truncate text-sm text-fg-3" title={meta}>{meta}</span>
				{/if}
			</div>
		{/if}
	</button>
</li>
