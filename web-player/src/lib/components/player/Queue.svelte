<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import ArtistLink from '$lib/components/ui/ArtistLink.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import { pressable } from '$lib/actions/pressable';
	import { fmtTime } from '$lib/format';
	import X from '@lucide/svelte/icons/x';
	import { fly } from 'svelte/transition';
	import { cubicOut } from 'svelte/easing';

	const upcoming = $derived.by(() => {
		const idx = player.tracks.findIndex((t) => t.id === player.currentId);
		return idx === -1 ? player.tracks : player.tracks.slice(idx + 1);
	});
</script>

{#if queuePanel.open}
	<aside
		transition:fly={{ x: 24, duration: 220, easing: cubicOut }}
		class="hidden shrink-0 flex-col border-l border-hairline bg-bg px-4 pt-5 transition-[background] duration-500 sm:flex"
		style="width:var(--mf-queue-w); padding-bottom:calc(var(--mf-player-h) + 2rem)"
	>
		<div class="mb-5 flex items-center justify-between">
			<h2 class="font-display text-base font-medium tracking-tight text-fg">En cola</h2>
			<IconButton label="Cerrar cola" tone="plain" size="xs" onclick={() => queuePanel.close()}>
				<X class="size-icon-md" />
			</IconButton>
		</div>

		{#if player.current.id}
			<p class="mb-2.75 text-xs font-medium tracking-widest text-fg-3 uppercase">Reproduciendo</p>
			<div class="mb-6 flex items-center gap-3 rounded-xl bg-accent-tint p-2.5">
				<Artwork
					trackIds={[player.current.id]}
					size="sm"
					alt={player.current.title}
					class="shrink-0"
				/>
				<div class="min-w-0">
					<div class="truncate text-sm text-accent-soft">
						{player.current.title}
					</div>
					<ArtistLink
						name={player.current.artist || '—'}
						ownerUserId={player.current.ownerUserId}
						class="mt-0.5 text-xs text-fg-3"
					/>
				</div>
			</div>
		{/if}

		<div class="mb-2.75 flex items-center justify-between gap-3">
			<span class="min-w-0 flex-1 truncate text-xs font-medium tracking-widest text-fg-3 uppercase">
				A continuación
			</span>
			{#if upcoming.length > 0}
				<button
					type="button"
					onclick={() => player.clearUpcoming()}
					class="shrink-0 text-xs text-fg-3 transition-colors hover:text-fg"
				>
					Vaciar
				</button>
			{/if}
		</div>

		<div class="flex flex-1 flex-col gap-px overflow-y-auto">
			{#each upcoming as track, i (track.id)}
				{@const index = player.tracks.length - upcoming.length + i}
				<div
					role="button"
					tabindex="0"
					use:pressable={() => player.playQueueIndex(index)}
					class="flex items-center gap-3 rounded-control p-2.25 text-left hover:bg-hover"
				>
					<Artwork trackIds={[track.id]} size="sm" alt={track.title} class="shrink-0 opacity-80" />
					<div class="min-w-0 flex-1">
						<div class="truncate text-xs text-fg">{track.title}</div>
						<ArtistLink
							name={track.artist || '—'}
							ownerUserId={track.ownerUserId}
							class="mt-0.5 text-xs text-fg-3"
						/>
					</div>
					{#if track.duration}
						<span class="shrink-0 text-xs text-muted tabular-nums">{fmtTime(track.duration)}</span>
					{/if}
				</div>
			{:else}
				<p class="px-2 py-6 text-center text-xs text-fg-3">No hay más canciones en la cola.</p>
			{/each}
		</div>
	</aside>
{/if}
