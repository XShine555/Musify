<script lang="ts">
	import LibraryUploadsSection from '$lib/components/music/library/LibraryUploadsSection.svelte';

	let { data } = $props();
</script>

<LibraryUploadsSection genres={data.genres} albums={data.albums} />
