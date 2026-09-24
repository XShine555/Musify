<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { isQueueCurrent, playAllOrToggle } from '$lib/player/actions';
	import type { Track } from '$lib/types';
	import Button from '$lib/components/ui/primitives/Button.svelte';

	interface Props {
		items: Track[];
		load?: () => Promise<Track[]>;
		variant?: 'primary' | 'secondary' | 'accent';
		size?: 'sm' | 'md' | 'lg';
		idleLabel?: string;
		activeLabel?: string;
	}

	let {
		items,
		load,
		variant = 'primary',
		size = 'sm',
		idleLabel = 'Reproducir',
		activeLabel = 'Pausar'
	}: Props = $props();

	const active = $derived(isQueueCurrent(items) && player.playing);

	async function play() {
		if (load && !isQueueCurrent(items)) playAllOrToggle(await load());
		else playAllOrToggle(items);
	}
</script>

<Button {variant} {size} onclick={play} disabled={items.length === 0 && !load}>
	{active ? activeLabel : idleLabel}
</Button>
