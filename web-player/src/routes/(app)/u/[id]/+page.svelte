<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Page from '$lib/components/ui/Page.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import PlaylistCard from '$lib/components/ui/PlaylistCard.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import ArtistAvatar from '$lib/components/ui/ArtistAvatar.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';

	let { data, form } = $props();

	const profile = $derived(data.profile);
	const playlists = $derived(data.playlists);
	const canFollow = $derived(!data.isOwnProfile && !data.isAnonymous);

	let following = $state(untrack(() => data.isFollowing));
	let submitting = $state(false);

	$effect(() => {
		if (form && 'following' in form && typeof form.following === 'boolean')
			following = form.following;
	});
</script>

<svelte:head>
	<title>{profile.name}</title>
	<meta name="description" content="Perfil de {profile.name} en Musify." />
</svelte:head>

{#snippet followAction()}
	<form
		method="POST"
		action={following ? '?/unfollow' : '?/follow'}
		use:enhance={() => {
			submitting = true;
			return async ({ update }) => {
				await update();
				submitting = false;
			};
		}}
	>
		<Button
			type="submit"
			size="sm"
			variant={following ? 'secondary' : 'primary'}
			disabled={submitting}
		>
			{following ? 'Dejar de seguir' : 'Seguir'}
		</Button>
	</form>
{/snippet}

<Page>
	<CollectionHeader
		eyebrow="Perfil"
		title={profile.name}
		meta="{profile.followersCount} {profile.followersCount === 1
			? 'seguidor'
			: 'seguidores'} · {profile.followingCount} siguiendo"
		align="center"
		actions={canFollow ? followAction : undefined}
	>
		{#snippet cover()}
			<ArtistAvatar name={profile.name} imageUrl={profile.profilePictureUrl} size={144} />
		{/snippet}
	</CollectionHeader>

	<div class="mt-9">
		<SectionHeading title="Playlists públicas" subtitle="Creadas y compartidas por este usuario" />

		{#if playlists.length > 0}
			<MediaGrid min="210px" minMobile="180px" class="mt-6">
				{#each playlists as playlist, i (playlist.id)}
					<PlaylistCard
						id={playlist.id}
						name={playlist.name}
						trackCount={playlist.trackCount}
						trackIds={playlist.coverTrackIds}
						updatedAt={playlist.updatedAt}
						index={i}
					/>
				{/each}
			</MediaGrid>
		{:else}
			<EmptyState
				icon={ListMusic}
				title="Sin playlists públicas"
				description="Este usuario todavía no ha compartido ninguna playlist pública."
				class="mt-6"
			/>
		{/if}
	</div>
</Page>
