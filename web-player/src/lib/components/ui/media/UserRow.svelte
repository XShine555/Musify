<script lang="ts">
	import ListRow from './ListRow.svelte';
	import Avatar from './Avatar.svelte';
	import FollowButton from './FollowButton.svelte';

	interface Props {
		user: {
			id: string;
			name: string;
			profilePictureUrl: string | null;
			isFollowedByViewer: boolean;
		};
		canFollow?: boolean;
	}

	let { user, canFollow = false }: Props = $props();
</script>

<div class="flex items-center gap-3">
	<ListRow title={user.name} href="/user/{user.id}" size="lg" class="min-w-0 flex-1">
		{#snippet art()}
			<Avatar name={user.name} src={user.profilePictureUrl} size="md" />
		{/snippet}
	</ListRow>
	{#if canFollow}
		<FollowButton following={user.isFollowedByViewer} userId={user.id} class="shrink-0" />
	{/if}
</div>
