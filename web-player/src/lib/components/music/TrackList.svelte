<script lang="ts" module>
	import type { Track } from '$lib/types';

	export type TrackListColumn = 'added' | 'uploaded' | 'plays';

	export type TrackListTrack = Track & { date?: string | number };

	const COLUMN_PRESET: Record<TrackListColumn, { label: string; width: string }> = {
		added: { label: 'Añadida', width: '124px' },
		uploaded: { label: 'Subida', width: '124px' },
		plays: { label: 'Escuchas', width: '104px' }
	};

	const INDEX_W = { desktop: '32px', mobile: '28px' };
	const TITLE_W = { desktop: 'minmax(180px,1fr)', mobile: 'minmax(120px,1fr)' };
	const DURATION_W = { desktop: '104px', mobile: '52px' };
	const ACTION_W = { desktop: '36px', mobile: '32px' };
</script>

<script lang="ts">
	import { enhance } from '$app/forms';
	import type { LucideIcon } from '@lucide/svelte';
	import Hash from '@lucide/svelte/icons/hash';
	import Play from '@lucide/svelte/icons/play';
	import { player } from '$lib/player/player.svelte';
	import { fmtTime, fmtDate } from '$lib/utils/format';
	import { pressable } from '$lib/actions/pressable';
	import MediaIdentity from '$lib/components/ui/media/MediaIdentity.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';

	interface RowAction {
		action?: string;
		onclick?: (track: TrackListTrack) => void;
		icon: LucideIcon;
		label: string;
	}

	interface Props {
		tracks: TrackListTrack[];
		columns?: TrackListColumn[];
		index?: boolean;
		onPlay: (index: number) => void;
		rowAction?: RowAction;
		oncontextmenu?: (event: MouseEvent, track: TrackListTrack, index: number) => void;
		class?: string;
	}

	let {
		tracks,
		columns = [],
		index = true,
		onPlay,
		rowAction,
		oncontextmenu,
		class: klass = ''
	}: Props = $props();

	const gridColumns = $derived(
		[
			index ? INDEX_W.desktop : '',
			TITLE_W.desktop,
			...columns.map((c) => COLUMN_PRESET[c].width),
			DURATION_W.desktop,
			rowAction ? ACTION_W.desktop : ''
		]
			.filter(Boolean)
			.join(' ')
	);

	const gridColumnsMobile = $derived(
		[
			index ? INDEX_W.mobile : '',
			TITLE_W.mobile,
			DURATION_W.mobile,
			rowAction ? ACTION_W.mobile : ''
		]
			.filter(Boolean)
			.join(' ')
	);
</script>

<div class="overflow-x-auto {klass}">
	<div
		class="grid track-grid items-center gap-3 border-b border-line pr-4 pb-3 pl-2 text-sm text-fg-2 sm:gap-8 sm:pr-6 sm:pl-3"
		style="--mf-track-cols:{gridColumns}; --mf-track-cols-mobile:{gridColumnsMobile}"
	>
		{#if index}
			<Hash class="size-icon-sm" />
		{/if}
		<span>Título</span>
		{#each columns as column (column)}
			<span class="hidden text-center sm:block">{COLUMN_PRESET[column].label}</span>
		{/each}
		<span class="hidden text-center sm:block">Duración</span>
		{#if rowAction}
			<span></span>
		{/if}
	</div>
	<div class="mt-1 flex flex-col gap-0.5" role="list">
		{#each tracks as track, i (track.id)}
			{@const active = player.currentId === track.id}

			<div
				role="listitem"
				class="group grid track-grid items-center gap-3 rounded-control py-3 pr-4 pl-2 transition-colors sm:gap-8 sm:pr-6 sm:pl-3 {active
					? 'surface-active'
					: 'hover:bg-hover'}"
				style="--mf-track-cols:{gridColumns}; --mf-track-cols-mobile:{gridColumnsMobile}"
				oncontextmenu={oncontextmenu ? (event) => oncontextmenu(event, track, i) : undefined}
			>
				{#if index}
					<button
						type="button"
						onclick={() => onPlay(i)}
						aria-label={active && player.playing ? 'Pausar' : 'Reproducir'}
						class="relative grid size-8 place-items-center rounded-tag text-xs text-fg-2 tabular-nums"
					>
						{#if active}
							<EqBars paused={!player.playing} />
						{:else}
							<span class="group-hover:hidden">{i + 1}</span>
							<Play class="hidden size-icon-sm text-fg group-hover:block" fill="currentColor" />
						{/if}
					</button>
				{/if}

				<div
					role="button"
					tabindex="0"
					use:pressable={() => onPlay(i)}
					class="flex min-w-0 flex-1 text-left"
				>
					<MediaIdentity
						trackId={track.id}
						title={track.title}
						artist={track.artist}
						ownerUserId={track.ownerUserId}
						explicit={track.explicit}
						{active}
					>
						{#snippet overlay()}
							{#if !index && active}
								<EqBars overlay paused={!player.playing} />
							{/if}
						{/snippet}
					</MediaIdentity>
				</div>
				{#each columns as column (column)}
					<span class="hidden truncate text-center text-xs text-fg-2 tabular-nums sm:block">
						{#if column === 'plays'}
							{track.listensCount}
						{:else if track.date}
							{fmtDate(track.date)}
						{/if}
					</span>
				{/each}
				<span class="truncate text-center text-xs text-fg-2 tabular-nums">
					{fmtTime(track.duration)}
				</span>
				{#if rowAction}
					{#if rowAction.action}
						<form method="POST" action={rowAction.action} use:enhance>
							<input type="hidden" name="trackId" value={track.id} />
							<IconButton type="submit" label={rowAction.label} size="xs" revealOnHover>
								<rowAction.icon class="size-icon-sm" />
							</IconButton>
						</form>
					{:else}
						<IconButton
							label={rowAction.label}
							size="xs"
							revealOnHover
							onclick={() => rowAction.onclick?.(track)}
						>
							<rowAction.icon class="size-icon-sm" />
						</IconButton>
					{/if}
				{/if}
			</div>
		{/each}
	</div>
</div>
