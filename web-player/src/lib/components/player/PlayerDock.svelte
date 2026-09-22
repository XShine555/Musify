<script lang="ts">
	import PlayerBar from './PlayerBar.svelte';
	import TabsBar from '../layout/TabsBar.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import { player } from '$lib/player/player.svelte';
	import type { SessionUser } from '$lib/types';

	interface Props {
		user: SessionUser | null;
	}

	let { user }: Props = $props();

	const hasTrack = $derived(player.currentId !== null);
</script>

<div
	class="pointer-events-none fixed inset-x-0 bottom-0 z-(--z-sticky) flex flex-col lg:left-(--mf-sidebar-w)"
>
	{#if player.error}
		<div class="pointer-events-auto px-4 pb-2.5">
			<Alert tone="danger">{player.error}</Alert>
		</div>
	{/if}
	{#if hasTrack}
		<PlayerBar />
	{/if}
	<TabsBar {user} />
</div>
