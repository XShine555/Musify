<script lang="ts" module>
	export type TrackListColumn = 'added' | 'uploaded' | 'plays';

	export interface TrackListTrack {
		id: string | number;
		title: string;
		artist?: string | null;
		ownerUserId?: string | number | null;
		isExplicit?: boolean;
		explicit?: boolean;
		duration?: number | string;
		listensCount?: number | string;
		addedAt?: string | number;
		uploadedAt?: string | number;
	}

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
	import { fmtTime, fmtDate } from '$lib/format';
	import { pressable } from '$lib/actions/pressable';
	import MediaIdentity from './MediaIdentity.svelte';
	import EqBars from './EqBars.svelte';
	import IconButton from './IconButton.svelte';

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
		class="grid track-grid items-center gap-3 border-b border-line pr-4 pb-3 pl-2 text-sm text-fg-3 sm:gap-8 sm:pr-6 sm:pl-3"
		style="--mf-track-cols:{gridColumns}; --mf-track-cols-mobile:{gridColumnsMobile}"
	>
		{#if index}
			<Hash size={16} strokeWidth={1.5} />
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
	<div class="mt-1 flex flex-col gap-0.5">
		{#each tracks as track, i (track.id)}
			{@const active = player.current.id === track.id}
			<!-- svelte-ignore a11y_no_static_element_interactions -->
			<div
				class="group grid track-grid items-center gap-3 rounded-control py-3 pr-4 pl-2 transition-colors sm:gap-8 sm:pr-6 sm:pl-3 {active
					? 'bg-accent-tint'
					: 'hover:bg-hover'}"
				style="--mf-track-cols:{gridColumns}; --mf-track-cols-mobile:{gridColumnsMobile}"
				oncontextmenu={oncontextmenu ? (event) => oncontextmenu(event, track, i) : undefined}
			>
				{#if index}
					<button
						type="button"
						onclick={() => onPlay(i)}
						aria-label={active && player.playing ? 'Pausar' : 'Reproducir'}
						class="relative grid size-8 place-items-center rounded-tag text-xs text-muted tabular-nums"
					>
						{#if active}
							<EqBars paused={!player.playing} />
						{:else}
							<span class="group-hover:hidden">{i + 1}</span>
							<Play class="hidden size-icon-sm text-fg group-hover:block" fill="currentColor" />
						{/if}
					</button>
				{/if}
				<!-- svelte-ignore a11y_click_events_have_key_events -->
				<div
					role="button"
					tabindex="0"
					use:pressable={() => onPlay(i)}
					onclick={(event) => event.stopPropagation()}
					class="flex min-w-0 flex-1 text-left"
				>
					<MediaIdentity
						trackId={track.id}
						title={track.title}
						artist={track.artist}
						ownerUserId={track.ownerUserId}
						explicit={track.isExplicit ?? track.explicit ?? false}
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
					<span class="hidden truncate text-center text-xs text-muted tabular-nums sm:block">
						{#if column === 'plays'}
							{Number(track.listensCount ?? 0)}
						{:else if column === 'added' && track.addedAt}
							{fmtDate(track.addedAt)}
						{:else if column === 'uploaded' && track.uploadedAt}
							{fmtDate(track.uploadedAt)}
						{/if}
					</span>
				{/each}
				<span class="truncate text-center text-xs text-muted tabular-nums">
					{fmtTime(Number(track.duration))}
				</span>
				{#if rowAction}
					{#if rowAction.action}
						<form
							method="POST"
							action={rowAction.action}
							use:enhance={() =>
								async ({ update }) =>
									update()}
						>
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
