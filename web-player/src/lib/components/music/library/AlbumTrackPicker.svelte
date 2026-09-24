<script lang="ts">
	import { untrack } from 'svelte';
	import Plus from '@lucide/svelte/icons/plus';
	import X from '@lucide/svelte/icons/x';
	import type { LibraryTrack } from '$lib/types';
	import { plural } from '$lib/utils/format';
	import { transferBetweenLists } from '$lib/utils/transitions';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import TrackPickList from './TrackPickList.svelte';

	interface Props {
		tracks: LibraryTrack[];
		memberIds: string[];
		onchange?: () => void;
	}

	let { tracks, memberIds, onchange }: Props = $props();

	const [send, receive] = transferBetweenLists();

	let currentIds = $state<string[]>(untrack(() => [...memberIds]));

	const initialIds = untrack(() => new Set(memberIds));
	const current = $derived(new Set(currentIds));
	const inAlbum = $derived(tracks.filter((track) => current.has(track.id)));
	const available = $derived(tracks.filter((track) => !current.has(track.id)));
	const addedIds = $derived(currentIds.filter((id) => !initialIds.has(id)));
	const removedIds = $derived([...initialIds].filter((id) => !current.has(id)));

	function add(track: LibraryTrack) {
		currentIds = [...currentIds, track.id];
		onchange?.();
	}

	function remove(track: LibraryTrack) {
		currentIds = currentIds.filter((id) => id !== track.id);
		onchange?.();
	}
</script>

<input type="hidden" name="addTrackIds" value={addedIds.join(',')} />
<input type="hidden" name="removeTrackIds" value={removedIds.join(',')} />

<div class="min-w-0">
	<h2 class="text-display-3">Canciones</h2>
	<p class="mt-1 text-sm text-fg-2">
		{inAlbum.length > 0 ? plural(inAlbum.length, 'canción', 'canciones') : 'Sin canciones'}
	</p>

	{#if available.length > 0}
		<h3 class="mt-6 mb-2 px-3 text-eyebrow text-fg-2">Añadir desde tu biblioteca</h3>
		<TrackPickList tracks={available} icon={Plus} {send} {receive} onPick={add} />
	{/if}

	<div class="mt-6">
		{#if inAlbum.length > 0}
			<h3 class="mb-2 px-3 text-eyebrow text-fg-2">En este álbum</h3>
			<TrackPickList tracks={inAlbum} icon={X} {send} {receive} onPick={remove} />
		{:else}
			<EmptyState
				title="Este álbum aún no tiene canciones"
				description="Añádelas desde tu biblioteca o sube archivos nuevos."
			/>
		{/if}
	</div>
</div>
