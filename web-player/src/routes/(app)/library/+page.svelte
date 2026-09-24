<script lang="ts">
	import LibraryTracksSection from '$lib/components/music/library/LibraryTracksSection.svelte';
	import { plural } from '$lib/utils/format';

	let { data, form } = $props();

	const errorMessage = $derived(
		form && 'message' in form && form.action !== 'saveAlbum' ? form.message : undefined
	);
	const published = $derived(
		form?.action === 'setPublished' ? (form as { skipped?: number; isPublished?: boolean }) : null
	);
	const notice = $derived(
		published?.skipped
			? `${plural(published.skipped, 'canción', 'canciones')} sin procesar: no se ${published.isPublished ? 'pueden publicar' : 'han despublicado'} todavía.`
			: undefined
	);
</script>

<LibraryTracksSection
	tracks={data.tracks}
	counts={data.counts}
	query={data.q}
	filter={data.filter}
	{errorMessage}
	{notice}
/>
