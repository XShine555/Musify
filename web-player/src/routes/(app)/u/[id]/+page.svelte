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

	let { data, form } = $props();

	const profile = $derived(data.profile);
	const playlists = $derived(data.playlists);

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

<Page>
	<div class="flex flex-col gap-5 sm:flex-row sm:items-center sm:gap-6">
		<ArtistAvatar name={profile.name} imageUrl={profile.profilePictureUrl} size={144} />
		<div class="min-w-0 flex-1">
			<p class="text-[10.5px] font-semibold tracking-[0.16em] text-fg-2 uppercase">Perfil</p>
			<h1
				class="mt-3 font-display text-[28px] font-semibold tracking-[-0.035em] break-words text-fg sm:text-[33px]"
			>
				{profile.name}
			</h1>
			<p class="mt-3.25 text-[12.5px] text-fg-2">
				{profile.followersCount}
				{profile.followersCount === 1 ? 'seguidor' : 'seguidores'} · {profile.followingCount} siguiendo
			</p>
		</div>
	</div>

	{#if !data.isOwnProfile && !data.isAnonymous}
		<div class="mt-6 flex items-center gap-2.5 sm:mt-7">
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
		</div>
	{/if}

	<div class="mt-9">
		<h2 class="font-display text-lg font-semibold tracking-[-0.02em] text-fg">
			Playlists públicas
		</h2>

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
