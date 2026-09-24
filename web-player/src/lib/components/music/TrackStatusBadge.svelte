<script lang="ts">
	import type { LibraryTrack } from '$lib/types';
	import { trackState } from '$lib/data/libraryTracks';
	import StatusBadge from '$lib/components/ui/media/StatusBadge.svelte';

	interface Props {
		track: Pick<LibraryTrack, 'status' | 'published'>;
		class?: string;
	}

	let { track, class: klass = '' }: Props = $props();

	const info = $derived(trackState(track));
</script>

<StatusBadge label={info.label} tone={info.tone} pulse={info.pulse} class={klass} />
