<script lang="ts">
	import { player } from '$lib/player/player.svelte';
</script>

<div class="flex min-w-0 items-center gap-3">
	<div
		class="grid h-12 w-12 shrink-0 place-items-center rounded-lg bg-gradient-to-br from-emerald-500/25 to-emerald-500/5 text-xl"
	>
		🎵
	</div>
	<div class="min-w-0">
		<p class="truncate text-sm font-semibold">{player.current?.title}</p>
		{#if player.error}
			<p class="truncate text-xs text-red-400">{player.error}</p>
		{:else}
			<p class="truncate text-xs text-neutral-500">Musify</p>
		{/if}
	</div>
</div>
