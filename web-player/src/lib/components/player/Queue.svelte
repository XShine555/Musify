<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import ArtistLink from '$lib/components/ui/ArtistLink.svelte';
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
		class="hidden shrink-0 flex-col border-l border-hairline bg-[image:var(--mf-panel-bg)] px-4 pt-5 transition-[background] duration-[600ms] sm:flex"
		style="width:var(--mf-queue-w); padding-bottom:calc(var(--mf-player-h) + 2rem)"
	>
		<div class="mb-5 flex items-center justify-between">
			<h2 class="font-display text-[14.5px] font-semibold tracking-[-0.02em] text-fg">En cola</h2>
			<button
				type="button"
				onclick={() => queuePanel.close()}
				aria-label="Cerrar cola"
				class="text-base text-fg-3 transition-colors hover:text-fg"
			>
				<X class="h-4 w-4" />
			</button>
		</div>

		{#if player.current.id}
			<p class="mb-2.75 text-[10.5px] font-semibold tracking-[0.12em] text-muted uppercase">
				Reproduciendo
			</p>
			<div class="mb-6 flex items-center gap-3 rounded-xl bg-accent-tint p-2.5">
				<Cover
					trackId={player.current.id}
					src={player.current.coverUrl}
					size="small"
					alt={player.current.title}
					class="h-10 w-10 shrink-0 rounded-[9px]"
				/>
				<div class="min-w-0">
					<div class="truncate text-[12.5px] font-semibold text-accent-soft">
						{player.current.title}
					</div>
					<ArtistLink
						name={player.current.artist || '—'}
						ownerUserId={player.current.ownerUserId}
						class="mt-0.75 text-[11px] text-fg-3"
					/>
				</div>
			</div>
		{/if}

		<div class="mb-2.75 flex items-center justify-between gap-3">
			<span
				class="min-w-0 flex-1 truncate text-[10.5px] font-semibold tracking-[0.12em] text-muted uppercase"
			>
				A continuación
			</span>
			{#if upcoming.length > 0}
				<button
					type="button"
					onclick={() => player.clearUpcoming()}
					class="shrink-0 text-[11px] text-muted transition-colors hover:text-fg"
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
					onclick={() => player.playQueueIndex(index)}
					onkeydown={(e) => {
						if (e.key !== 'Enter' && e.key !== ' ') return;
						e.preventDefault();
						player.playQueueIndex(index);
					}}
					class="flex items-center gap-3 rounded-[11px] p-2.25 text-left hover:bg-hover"
				>
					<Cover
						trackId={track.id}
						src={track.coverUrl}
						size="small"
						alt={track.title}
						class="h-9 w-9 shrink-0 rounded-lg opacity-90"
					/>
					<div class="min-w-0 flex-1">
						<div class="truncate text-[12.5px] font-medium text-fg">{track.title}</div>
						<ArtistLink
							name={track.artist || '—'}
							ownerUserId={track.ownerUserId}
							class="mt-0.5 text-[11px] text-fg-3"
						/>
					</div>
					{#if track.duration}
						<span class="shrink-0 text-[11px] text-muted tabular-nums"
							>{fmtTime(track.duration)}</span
						>
					{/if}
				</div>
			{:else}
				<p class="px-2 py-6 text-center text-xs text-fg-3">No hay más canciones en la cola.</p>
			{/each}
		</div>
	</aside>
{/if}
