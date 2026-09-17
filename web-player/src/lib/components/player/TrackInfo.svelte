<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
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
			ownerUserId: player.current.ownerUserId
		});
	}
</script>

<div class="flex min-w-0 items-center gap-2.5 md:gap-3.25">
	{#if player.current.id}
		<Cover
			trackId={player.current.id}
			size="small"
			alt={player.current.title}
			class="h-11 w-11 shrink-0 rounded-xl md:h-12 md:w-12"
		/>
	{:else}
		<div class="h-11 w-11 shrink-0 rounded-xl bg-surface md:h-12 md:w-12"></div>
	{/if}
	<div class="min-w-0 flex-1">
		<div class="flex min-w-0 items-center gap-1.5">
			{#if player.current.explicit}
				<ExplicitBadge />
			{/if}
			<span class="truncate text-sm font-semibold tracking-[-0.01em] text-fg"
				>{player.current.title}</span
			>
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
