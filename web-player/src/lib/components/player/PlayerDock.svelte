<script lang="ts">
	import PlayerBar from './PlayerBar.svelte';
	import TabsBar from '$lib/components/shell/TabsBar.svelte';
	import { player } from '$lib/player/player.svelte';
	import type { SessionUser } from '$lib/types';

	interface Props {
		user: SessionUser | null;
	}

	let { user }: Props = $props();

	const hasTrack = $derived(player.currentId !== null);
</script>

<div class="flex shrink-0 flex-col">
	{#if hasTrack}
		<PlayerBar />
	{/if}
	<TabsBar {user} />
</div>
