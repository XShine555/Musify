<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import MediaIdentity from '$lib/components/ui/media/MediaIdentity.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import { fmtTime } from '$lib/utils/format';
	import { flip } from 'svelte/animate';
	import { expoOut } from 'svelte/easing';
	import X from '@lucide/svelte/icons/x';

	const upcoming = $derived.by(() => {
		const idx = player.tracks.findIndex((t) => t.id === player.currentId);
		return idx === -1 ? player.tracks : player.tracks.slice(idx + 1);
	});

	let dragFrom = $state<number | null>(null);
	let dropAt = $state<number | null>(null);

	function onDragOver(event: DragEvent, index: number) {
		if (dragFrom === null) return;
		event.preventDefault();
		if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
		const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
		dropAt = event.clientY < rect.top + rect.height / 2 ? index : index + 1;
	}

	function onDrop(event: DragEvent) {
		event.preventDefault();
		if (dragFrom !== null && dropAt !== null) player.moveQueueItem(dragFrom, dropAt);
		endDrag();
	}

	function endDrag() {
		dragFrom = null;
		dropAt = null;
	}
</script>

<div
	class="queue-shell max-sm:hidden"
	data-open={queuePanel.open ? '' : undefined}
	inert={!queuePanel.open}
>
	<aside
		class="flex min-h-0 shrink-0 flex-col border-l border-hairline px-4 pt-5 pb-5"
		style="width:var(--mf-queue-w)"
	>
		<div class="mb-5 flex items-center justify-between">
			<h2 class="font-display text-base font-medium tracking-tight text-fg">En cola</h2>
			<IconButton label="Cerrar cola" plain size="xs" onclick={() => queuePanel.close()}>
				<X class="size-icon-md" />
			</IconButton>
		</div>

		{#if player.current.id}
			<p class="mb-3 text-eyebrow text-fg-2">Reproduciendo</p>
			<div class="mb-6 surface-active rounded-control p-2.5">
				<MediaIdentity
					trackId={player.current.id}
					title={player.current.title}
					artist={player.current.artist || '—'}
					ownerUserId={player.current.ownerUserId}
					explicit={player.current.explicit}
					active
				/>
			</div>
		{/if}

		<div class="mb-3 flex items-center justify-between gap-3">
			<span class="min-w-0 flex-1 truncate text-eyebrow text-fg-2"> A continuación </span>
			{#if upcoming.length > 0}
				<button
					type="button"
					onclick={() => player.clearUpcoming()}
					class="shrink-0 text-xs text-fg-2 transition-colors hover:text-fg"
				>
					Vaciar
				</button>
			{/if}
		</div>

		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div
			class="flex min-h-0 flex-1 flex-col gap-px overflow-y-auto"
			ondragover={(event) => dragFrom !== null && event.preventDefault()}
			ondrop={onDrop}
		>
			{#each upcoming as track, i (track.id)}
				{@const index = player.tracks.length - upcoming.length + i}
				<!-- svelte-ignore a11y_no_static_element_interactions -->
				<div
					draggable="true"
					ondragstart={(event) => {
						dragFrom = index;
						if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
					}}
					ondragover={(event) => onDragOver(event, index)}
					ondrop={onDrop}
					ondragend={endDrag}
					class="queue-row"
					data-dragging={dragFrom === index ? '' : undefined}
					data-drop={dragFrom === null
						? undefined
						: dropAt === index
							? 'before'
							: dropAt === index + 1 && index === player.tracks.length - 1
								? 'after'
								: undefined}
					animate:flip={{ duration: 300, easing: expoOut }}
				>
					{#if dropAt === index && dragFrom !== null}
						<div
							class="pointer-events-none absolute inset-x-2 -top-7 h-0.5 rounded-full bg-accent"
						></div>
					{:else if dropAt === index + 1 && dropAt === player.tracks.length && dragFrom !== null}
						<div
							class="pointer-events-none absolute inset-x-2 -bottom-7 h-0.5 rounded-full bg-accent"
						></div>
					{/if}
					<ListRow
						onclick={() => player.playQueueIndex(index)}
						size="sm"
						title={track.title}
						subtitle={track.artist || '—'}
						subtitleHref={track.ownerUserId}
						explicit={track.explicit}
						trackId={track.id}
						class="p-2"
					>
						{#snippet trailing()}
							{#if track.duration}
								<span class="shrink-0 text-xs text-fg-2 tabular-nums"
									>{fmtTime(track.duration)}</span
								>
							{/if}
						{/snippet}
					</ListRow>
				</div>
			{:else}
				<p class="px-2 py-6 text-center text-xs text-fg-2">No hay más canciones en la cola.</p>
			{/each}
		</div>
	</aside>
</div>
