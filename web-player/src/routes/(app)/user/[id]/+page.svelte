<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import Avatar from '$lib/components/ui/media/Avatar.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import SectionHeading from '$lib/components/ui/layout/SectionHeading.svelte';
	import { plural, playlistMeta } from '$lib/utils/format';

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
	<PageHeader
		eyebrow="Perfil"
		title={profile.name}
		meta="{plural(
			Number(profile.followersCount),
			'seguidor',
			'seguidores'
		)} · {profile.followingCount} siguiendo"
		align="center"
		actions={canFollow ? followAction : undefined}
	>
		{#snippet cover()}
			<Avatar name={profile.name} src={profile.profilePictureUrl} size="hero" />
		{/snippet}
	</PageHeader>

	<div>
		<SectionHeading title="Playlists públicas" subtitle="Creadas y compartidas por este usuario" />

		{#if playlists.length > 0}
			<div class="mt-6 grid-cards-lg">
				{#each playlists as playlist, i (playlist.id)}
					<MediaCard
						href="/playlists/{playlist.id}"
						title={playlist.name}
						subtitle={playlistMeta(Number(playlist.trackCount))}
						trackIds={playlist.coverTrackIds}
						src="/api/playlists/{playlist.id}/cover?size=large&v={encodeURIComponent(
							playlist.updatedAt
						)}"
						index={i}
					/>
				{/each}
			</div>
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
