<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import MediaIdentity from '$lib/components/ui/media/MediaIdentity.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import { fmtTime } from '$lib/utils/format';
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
		class="hidden shrink-0 flex-col border-l border-hairline px-4 pt-5 sm:flex"
		style="width:var(--mf-queue-w); padding-bottom:calc(var(--mf-player-h) + 2rem)"
	>
		<div class="mb-5 flex items-center justify-between">
			<h2 class="font-display text-base font-medium tracking-tight text-fg">En cola</h2>
			<IconButton label="Cerrar cola" tone="plain" size="xs" onclick={() => queuePanel.close()}>
				<X class="size-icon-md" />
			</IconButton>
		</div>

		{#if player.current.id}
			<p class="mb-3 text-eyebrow text-fg-3">Reproduciendo</p>
			<div class="mb-6 rounded-control bg-accent-tint p-2.5">
				<MediaIdentity
					trackId={player.current.id}
					title={player.current.title}
					artist={player.current.artist || '—'}
					ownerUserId={player.current.ownerUserId}
					active
				/>
			</div>
		{/if}

		<div class="mb-3 flex items-center justify-between gap-3">
			<span class="min-w-0 flex-1 truncate text-eyebrow text-fg-3"> A continuación </span>
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
				<ListRow
					onclick={() => player.playQueueIndex(index)}
					size="sm"
					title={track.title}
					subtitle={track.artist || '—'}
					subtitleHref={track.ownerUserId}
					trackId={track.id}
					class="p-2"
				>
					{#snippet trailing()}
						{#if track.duration}
							<span class="shrink-0 text-xs text-muted tabular-nums">{fmtTime(track.duration)}</span
							>
						{/if}
					{/snippet}
				</ListRow>
			{:else}
				<p class="px-2 py-6 text-center text-xs text-fg-3">No hay más canciones en la cola.</p>
			{/each}
		</div>
	</aside>
{/if}
