<script lang="ts">
	import { goto } from '$app/navigation';
	import LibraryAlbumEditor from '$lib/components/music/library/LibraryAlbumEditor.svelte';

	let { data, form } = $props();
</script>

<div class="mt-4">
	<LibraryAlbumEditor
		album={null}
		tracks={data.tracks.items}
		memberIds={[]}
		formMessage={form?.action === 'saveAlbum' ? form.message : undefined}
		onClose={() => goto('/library/albums')}
	/>
</div>
