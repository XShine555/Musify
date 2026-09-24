<script lang="ts">
	import LibraryTracksSection from '$lib/components/music/library/LibraryTracksSection.svelte';

	let { data, form } = $props();
</script>

<LibraryTracksSection
	tracks={data.tracks.items}
	errorMessage={form?.action === 'deleteTracks' ? form.message : undefined}
/>
