<script lang="ts">
	import type { TrackStatus } from '$lib/types';
	import { TRACK_STATUS } from '$lib/data/libraryTracks';
	import StatusBadge from '$lib/components/ui/media/StatusBadge.svelte';

	interface Props {
		status: TrackStatus;
		class?: string;
	}

	let { status, class: klass = '' }: Props = $props();

	const info = $derived(TRACK_STATUS[status]);
</script>

<StatusBadge label={info.label} tone={info.tone} class={klass} />
