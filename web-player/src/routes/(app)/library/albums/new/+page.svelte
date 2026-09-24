<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import LibraryAlbumEditor from '$lib/components/music/library/LibraryAlbumEditor.svelte';

	let { data, form } = $props();
</script>

<div class="mt-4">
	<LibraryAlbumEditor
		album={null}
		artist={page.data.user?.name ?? ''}
		tracks={data.tracks.items}
		memberIds={[]}
		formMessage={form?.action === 'saveAlbum' ? form.message : undefined}
		onClose={() => goto('/library/albums')}
	/>
</div>
