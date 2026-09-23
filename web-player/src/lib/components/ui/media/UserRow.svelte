<script lang="ts">
	import ListRow from './ListRow.svelte';
	import Avatar from './Avatar.svelte';

	interface Props {
		user: {
			id: string;
			name: string;
			profilePictureUrl: string | null;
		};
	}

	let { user }: Props = $props();
</script>

<ListRow title={user.name} href="/user/{user.id}" size="lg" class="min-w-0 flex-1">
	{#snippet art()}
		<Avatar name={user.name} src={user.profilePictureUrl} size="md" />
	{/snippet}
</ListRow>
