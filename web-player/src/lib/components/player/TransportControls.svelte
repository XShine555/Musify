<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import Repeat from '@lucide/svelte/icons/repeat';

	interface Props {
		compact?: boolean;
	}

	let { compact = false }: Props = $props();

	const sideClass = $derived(
		compact
			? 'grid h-10 w-10 place-items-center text-fg opacity-70 transition active:scale-90'
			: 'flex text-fg-2 transition-colors hover:text-fg active:scale-90'
	);
</script>

<div class="flex shrink-0 items-center {compact ? 'gap-0.5' : 'gap-3.5'}">
	{#if !compact}
		<button
			type="button"
			onclick={() => player.toggleShuffle()}
			aria-pressed={player.shuffle}
			aria-label="Aleatorio"
			class="transition-colors hover:text-fg {player.shuffle ? 'text-accent' : 'text-fg-3'}"
		>
			<Shuffle class="h-3.75 w-3.75" strokeWidth={1.8} />
		</button>
	{/if}

	<button type="button" onclick={() => player.previous()} aria-label="Anterior" class={sideClass}>
		<svg width="16" height="14" viewBox="0 0 16 14" fill="currentColor">
			<rect x="0" y="0" width="2.5" height="14" rx="1" />
			<path d="M15 0 5 7l10 7z" />
		</svg>
	</button>

	<button
		type="button"
		onclick={() => player.toggle()}
		aria-label={player.playing ? 'Pausar' : 'Reproducir'}
		class="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-cta-strong text-ink transition duration-150 hover:brightness-95"
	>
		{#if player.playing}
			<svg width="15" height="15" viewBox="0 0 15 15" fill="currentColor">
				<rect x="3" y="1" width="3" height="13" rx="1" />
				<rect x="9" y="1" width="3" height="13" rx="1" />
			</svg>
		{:else}
			<svg width="15" height="15" viewBox="0 0 15 15" fill="currentColor">
				<path d="M3 1.5v12l10-6z" />
			</svg>
		{/if}
	</button>

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
			class="transition-colors hover:text-fg {player.repeat ? 'text-accent' : 'text-fg-3'}"
		>
			<Repeat class="h-3.75 w-3.75" strokeWidth={1.8} />
		</button>
	{/if}
</div>
