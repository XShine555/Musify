<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
</script>

<div class="flex min-w-0 items-center gap-3">
	{#if player.current.id}
		<Cover
			trackId={player.current.id}
			src={player.current.coverUrl}
			hue={player.current.hue}
			size="small"
			alt={player.current.title}
			class="h-13 w-13 shrink-0 rounded-lg shadow-[0_6px_16px_-6px_rgba(0,0,0,0.6)]"
		/>
	{:else}
		<div
			class="h-13 w-13 shrink-0 rounded-lg shadow-[0_6px_16px_-6px_rgba(0,0,0,0.6)]"
			style="background:{player.artGradient}"
		></div>
	{/if}
	<div class="min-w-0">
		<div class="flex min-w-0 items-center gap-1.5">
			{#if player.current.explicit}
				<ExplicitBadge />
			{/if}
			<span class="truncate text-fg">{player.current.title}</span>
		</div>
		<div class="truncate text-sm text-muted">{player.current.artist || '—'}</div>
	</div>
</div>
