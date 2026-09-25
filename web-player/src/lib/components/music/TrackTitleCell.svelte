<script lang="ts">
	import type { Track } from '$lib/types';
	import { player } from '$lib/player/player.svelte';
	import { pressable } from '$lib/actions/pressable';
	import MediaIdentity from '$lib/components/ui/media/MediaIdentity.svelte';
	import EqBars from '$lib/components/ui/media/EqBars.svelte';

	interface Props {
		track: Track;
		active: boolean;
		eqOverlay?: boolean;
		onPlay: () => void;
	}

	let { track, active, eqOverlay = true, onPlay }: Props = $props();
</script>

<div role="button" tabindex="0" use:pressable={onPlay} class="flex min-w-0 flex-1 text-left">
	<MediaIdentity
		trackId={track.id}
		title={track.title}
		artist={track.artist}
		ownerUserId={track.ownerUserId}
		coauthors={track.coauthors}
		explicit={track.explicit}
		{active}
	>
		{#snippet overlay()}
			{#if eqOverlay && active}
				<EqBars overlay paused={!player.playing} />
			{/if}
		{/snippet}
	</MediaIdentity>
</div>
