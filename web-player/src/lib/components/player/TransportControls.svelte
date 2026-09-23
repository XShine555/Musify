<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import PlayButton from '$lib/components/ui/media/PlayButton.svelte';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import Repeat from '@lucide/svelte/icons/repeat';

	interface Props {
		compact?: boolean;
	}

	let { compact = false }: Props = $props();

	const sideClass = $derived(
		compact
			? 'grid size-10 place-items-center text-fg opacity-70 transition active:scale-90'
			: 'flex text-fg-2 transition hover:text-fg active:scale-90'
	);
</script>

<div class="flex shrink-0 items-center {compact ? 'gap-0.5' : 'gap-3.5'}">
	{#if !compact}
		<button
			type="button"
			onclick={() => player.toggleShuffle()}
			aria-pressed={player.shuffle}
			aria-label="Aleatorio"
			class="transition-colors hover:text-fg {player.shuffle ? 'text-accent' : 'text-fg-2'}"
		>
			<Shuffle class="size-4" />
		</button>
	{/if}

	<button type="button" onclick={() => player.previous()} aria-label="Anterior" class={sideClass}>
		<svg width="16" height="14" viewBox="0 0 16 14" fill="currentColor">
			<rect x="0" y="0" width="2.5" height="14" rx="1" />
			<path d="M15 0 5 7l10 7z" />
		</svg>
	</button>

	<PlayButton
		playing={player.playing}
		onclick={() => player.toggle()}
		label={player.playing ? 'Pausar' : 'Reproducir'}
	/>

	<button type="button" onclick={() => player.next()} aria-label="Siguiente" class={sideClass}>
		<svg width="16" height="14" viewBox="0 0 16 14" fill="currentColor">
			<path d="M1 0 11 7 1 14z" />
			<rect x="13.5" y="0" width="2.5" height="14" rx="1" />
		</svg>
	</button>

	{#if !compact}
		<button
			type="button"
			onclick={() => player.toggleRepeat()}
			aria-pressed={player.repeat}
			aria-label="Repetir"
			class="transition-colors hover:text-fg {player.repeat ? 'text-accent' : 'text-fg-2'}"
		>
			<Repeat class="size-4" />
		</button>
	{/if}
</div>
