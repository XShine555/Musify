<script lang="ts">
	import type { LucideIcon } from '@lucide/svelte';
	import { flip } from 'svelte/animate';
	import { expoOut } from 'svelte/easing';
	import type { LibraryTrack } from '$lib/types';
	import type { transferBetweenLists } from '$lib/utils/transitions';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';

	type Transfer = ReturnType<typeof transferBetweenLists>;

	interface Props {
		tracks: LibraryTrack[];
		icon: LucideIcon;
		send: Transfer[0];
		receive: Transfer[1];
		onPick: (track: LibraryTrack) => void;
	}

	let { tracks, icon: Icon, send, receive, onPick }: Props = $props();

	function subtitle(track: LibraryTrack): string {
		const album = track.albumTitle ? `en ${track.albumTitle}` : 'Sin álbum';
		return track.artist ? `${track.artist} · ${album}` : album;
	}
</script>

<div class="flex flex-col gap-px" role="list">
	{#each tracks as track (track.id)}
		<div
			role="listitem"
			animate:flip={{ duration: 200, easing: expoOut }}
			in:receive|global={{ key: track.id }}
			out:send|global={{ key: track.id }}
		>
			<ListRow
				size="xs"
				trackId={track.id}
				title={track.title}
				subtitle={subtitle(track)}
				explicit={track.explicit}
				onclick={() => onPick(track)}
				class="px-3 py-2"
			>
				{#snippet trailing()}
					<Icon class="size-icon-sm shrink-0 text-fg-2" />
				{/snippet}
			</ListRow>
		</div>
	{/each}
</div>
