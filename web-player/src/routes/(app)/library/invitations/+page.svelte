<script lang="ts">
	import LibraryInvitationsSection from '$lib/components/music/library/LibraryInvitationsSection.svelte';

	let { data } = $props();
</script>

<LibraryInvitationsSection invitations={data.invitations.items} />
