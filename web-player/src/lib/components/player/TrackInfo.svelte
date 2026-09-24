<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { liked } from '$lib/state/liked.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import ExplicitBadge from '$lib/components/ui/media/ExplicitBadge.svelte';
	import ArtistLink from '$lib/components/ui/media/ArtistLink.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import Heart from '@lucide/svelte/icons/heart';

	interface Props {
		compact?: boolean;
	}

	let { compact = false }: Props = $props();

	const current = $derived(player.current);
	const isLiked = $derived(current !== null && liked.isLiked(current.id));

	function toggleLike() {
		if (current) liked.toggle(current);
	}
</script>

<div class="flex min-w-0 items-center gap-2.5 md:gap-3">
	{#if current}
		<Artwork
			trackIds={[current.id]}
			size="sm"
			alt={current.title}
			class="shrink-0 md:size-cover-md"
		/>
	{:else}
		<div class="size-cover-sm shrink-0 rounded-art bg-surface md:size-cover-md"></div>
	{/if}
	<div class="min-w-0">
		<div class="flex min-w-0 items-center gap-1.5">
			{#if current?.explicit}
				<ExplicitBadge />
			{/if}
			<span class="truncate text-sm text-fg">{current?.title ?? ''}</span>
		</div>
		<ArtistLink
			name={current?.artist || '—'}
			ownerUserId={current?.ownerUserId}
			class="mt-0.5 text-xs text-fg-2"
		/>
	</div>
	{#if !compact}
		<IconButton
			label={isLiked ? 'Quitar de Me gusta' : 'Añadir a Me gusta'}
			plain
			size="md"
			pressed={isLiked}
			onclick={toggleLike}
		>
			<Heart class="size-icon-md" fill={isLiked ? 'currentColor' : 'none'} />
		</IconButton>
	{/if}
</div>
