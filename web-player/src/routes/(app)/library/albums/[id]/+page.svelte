<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import LibraryAlbumEditor from '$lib/components/music/library/LibraryAlbumEditor.svelte';

	let { data, form } = $props();
</script>

<div class="mt-4">
	{#key data.album.id}
		<LibraryAlbumEditor
			album={data.album}
			artist={page.data.user?.name ?? ''}
			tracks={data.tracks.items}
			memberIds={data.memberIds}
			formMessage={form?.message}
			onClose={() => goto('/library/albums')}
		/>
	{/key}
</div>
