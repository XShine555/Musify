<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
	import ArtistLink from '$lib/components/ui/ArtistLink.svelte';
	import Heart from '@lucide/svelte/icons/heart';

	interface Props {
		compact?: boolean;
	}

	let { compact = false }: Props = $props();

	const isLiked = $derived(player.current.id !== '' && liked.isLiked(player.current.id));

	function toggleLike() {
		if (!player.current.id) return;
		liked.toggle({
			id: player.current.id,
			title: player.current.title,
			artist: player.current.artist,
			explicit: player.current.explicit,
			ownerUserId: player.current.ownerUserId,
			duration: player.current.duration
		});
	}
</script>

<div class="flex min-w-0 items-center gap-2.5 md:gap-3.25">
	{#if player.current.id}
		<Artwork
			trackIds={[player.current.id]}
			size="sm"
			alt={player.current.title}
			class="shrink-0 md:size-cover-md"
		/>
	{:else}
		<div class="size-cover-sm shrink-0 rounded-art bg-surface md:size-cover-md"></div>
	{/if}
	<div class="min-w-0 flex-1">
		<div class="flex min-w-0 items-center gap-1.5">
			{#if player.current.explicit}
				<ExplicitBadge />
			{/if}
			<span class="truncate text-sm text-fg">{player.current.title}</span>
		</div>
		<ArtistLink
			name={player.current.artist || '—'}
			ownerUserId={player.current.ownerUserId}
			class="mt-0.5 text-xs text-fg-3"
		/>
	</div>
	{#if !compact}
		<button
			type="button"
			onclick={toggleLike}
			aria-label={isLiked ? 'Quitar de Me gusta' : 'Añadir a Me gusta'}
			aria-pressed={isLiked}
			class="shrink-0 transition hover:opacity-80 {isLiked ? 'text-accent' : 'text-fg-3'}"
		>
			<Heart class="h-4 w-4" fill={isLiked ? 'currentColor' : 'none'} strokeWidth={1.7} />
		</button>
	{/if}
</div>
