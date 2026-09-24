<script lang="ts" module>
	type ColumnId = 'album' | 'genres' | 'status';

	const COLUMNS: { id: ColumnId; label: string; width: string }[] = [
		{ id: 'album', label: 'Álbum', width: 'minmax(96px,160px)' },
		{ id: 'genres', label: 'Géneros', width: 'minmax(96px,200px)' },
		{ id: 'status', label: 'Estado', width: '112px' }
	];

	const CHECK_W = '28px';
	const TITLE_W = { desktop: 'minmax(180px,1fr)', mobile: 'minmax(120px,1fr)' };
	const DURATION_W = { desktop: '80px', mobile: '52px' };
	const ACTION_W = { desktop: '36px', mobile: '32px' };
</script>

<script lang="ts">
	import X from '@lucide/svelte/icons/x';
	import type { LibraryTrack } from '$lib/types';
	import { player } from '$lib/player/player.svelte';
	import { genreInfo } from '$lib/data/genres';
	import { TRACK_STATUS } from '$lib/data/libraryTracks';
	import { fmtTime } from '$lib/utils/format';
	import { pressable } from '$lib/actions/pressable';
	import MediaIdentity from '$lib/components/ui/media/MediaIdentity.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';
	import StatusBadge from '$lib/components/ui/media/StatusBadge.svelte';
	import Checkbox from '$lib/components/ui/primitives/Checkbox.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';

	interface Props {
		tracks: LibraryTrack[];
		selectedIds: string[];
		onPlay: (index: number) => void;
		onDelete: (track: LibraryTrack) => void;
		class?: string;
	}

	let { tracks, selectedIds = $bindable(), onPlay, onDelete, class: klass = '' }: Props = $props();

	const selected = $derived(new Set(selectedIds));
	const allSelected = $derived(
		tracks.length > 0 && tracks.every((track) => selected.has(track.id))
	);

	const gridColumns = $derived(
		[
			CHECK_W,
			TITLE_W.desktop,
			...COLUMNS.map((c) => c.width),
			DURATION_W.desktop,
			ACTION_W.desktop
		].join(' ')
	);
	const gridColumnsMobile = $derived(
		[CHECK_W, TITLE_W.mobile, DURATION_W.mobile, ACTION_W.mobile].join(' ')
	);
	const gridStyle = $derived(
		`--mf-track-cols:${gridColumns}; --mf-track-cols-mobile:${gridColumnsMobile}`
	);

	function toggleAll(checked: boolean) {
		selectedIds = checked ? tracks.map((track) => track.id) : [];
	}

	function toggle(id: string, checked: boolean) {
		selectedIds = checked ? [...selectedIds, id] : selectedIds.filter((item) => item !== id);
	}

	function genresLabel(track: LibraryTrack): string {
		return track.genres.map((genre) => genreInfo(genre).label).join(', ') || '-';
	}
</script>

<div class="overflow-x-auto {klass}">
	<div
		class="grid track-grid items-center gap-3 border-b border-line pr-4 pb-3 pl-2 text-sm text-fg-2 sm:gap-6 sm:pr-6 sm:pl-3"
		style={gridStyle}
	>
		<Checkbox label="Seleccionar todas" bind:checked={() => allSelected, toggleAll} />
		<span>Título</span>
		{#each COLUMNS as column (column.id)}
			<span class="hidden sm:block">{column.label}</span>
		{/each}
		<span class="hidden text-center sm:block">Duración</span>
		<span></span>
	</div>
	<div class="mt-1 flex flex-col gap-0.5" role="list">
		{#each tracks as track, i (track.id)}
			{@const active = player.currentId === track.id}
			<div
				role="listitem"
				class="group grid track-grid items-center gap-3 rounded-control py-3 pr-4 pl-2 transition-colors sm:gap-6 sm:pr-6 sm:pl-3 {active
					? 'surface-active'
					: 'hover:bg-hover'}"
				style={gridStyle}
			>
				<Checkbox
					label="Seleccionar «{track.title}»"
					bind:checked={() => selected.has(track.id), (checked) => toggle(track.id, checked)}
				/>
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
							{#if active}
								<EqBars overlay paused={!player.playing} />
							{/if}
						{/snippet}
					</MediaIdentity>
				</div>
				{#each COLUMNS as column (column.id)}
					<span class="hidden min-w-0 truncate text-xs text-fg-2 sm:block">
						{#if column.id === 'album'}
							{track.albumTitle ?? '-'}
						{:else if column.id === 'genres'}
							{genresLabel(track)}
						{:else}
							{@const status = TRACK_STATUS[track.status]}
							<StatusBadge label={status.label} tone={status.tone} />
						{/if}
					</span>
				{/each}
				<span class="truncate text-center text-xs text-fg-2 tabular-nums">
					{fmtTime(track.duration)}
				</span>
				<IconButton label="Borrar canción" size="xs" revealOnHover onclick={() => onDelete(track)}>
					<X class="size-icon-sm" />
				</IconButton>
			</div>
		{/each}
	</div>
</div>
