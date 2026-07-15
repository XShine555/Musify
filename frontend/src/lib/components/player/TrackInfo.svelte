<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
</script>

<div class="flex min-w-0 items-center gap-3">
	{#if player.current.id}
		<Cover
			trackId={player.current.id}
			src={player.current.coverUrl}
			hue={player.current.hue}
			size="small"
			alt={player.current.title}
			class="h-[52px] w-[52px] flex-shrink-0 rounded-lg shadow-[0_6px_16px_-6px_rgba(0,0,0,0.6)]"
		/>
	{:else}
		<div
			class="h-[52px] w-[52px] flex-shrink-0 rounded-lg shadow-[0_6px_16px_-6px_rgba(0,0,0,0.6)]"
			style="background:{player.artGradient}"
		></div>
	{/if}
	<div class="min-w-0">
		<div class="truncate text-[13.5px] font-semibold text-[var(--mf-text)]">
			{player.current.title}
		</div>
		<div class="truncate text-xs text-[var(--mf-text-3)]">{player.current.artist || '—'}</div>
	</div>
</div>
