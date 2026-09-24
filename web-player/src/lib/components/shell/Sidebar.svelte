<script lang="ts">
	import { page } from '$app/state';
	import { appNavLinks, isNavActive } from './navLinks';
	import { playlistCover } from '$lib/utils/hrefs';
	import { liked } from '$lib/state/liked.svelte';
	import { createPlaylistModal } from '$lib/state/panels.svelte';
	import type { SessionUser } from '$lib/types';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import Plus from '@lucide/svelte/icons/plus';
	import Logo from '$lib/components/ui/primitives/Logo.svelte';

	const SIDEBAR_PLAYLISTS_LIMIT = 8;

	interface SidebarPlaylist {
		id: string;
		name: string;
		coverTrackIds: string[];
		updatedAt: string;
	}

	interface Props {
		user: SessionUser | null;
		playlists: SidebarPlaylist[];
		playlistCount: number;
	}

	let { user, playlists, playlistCount }: Props = $props();

	const visiblePlaylists = $derived(playlists.slice(0, SIDEBAR_PLAYLISTS_LIMIT));

	const navLinks = $derived(appNavLinks(user, { playlists: playlistCount, liked: liked.count }));
</script>

<aside
	class="hidden min-h-0 w-(--mf-sidebar-w) shrink-0 flex-col border-r border-hairline p-5 lg:flex"
>
	<a href="/" class="mb-4">
		<Logo size="sm" />
	</a>

	<nav class="flex flex-col gap-1">
		{#each navLinks as link (link.href)}
			{@const active = isNavActive(link.href)}
			<a
				href={link.href}
				aria-current={active ? 'page' : undefined}
				class="flex items-center gap-3 rounded-control px-3 py-2 text-sm transition-colors {active
					? 'bg-surface-2 text-fg'
					: 'text-fg-2 hover:bg-hover hover:text-fg'}"
			>
				<link.icon class="size-icon-md shrink-0" />
				<span class="flex-1 truncate">{link.label}</span>
				{#if link.count}
					<span class="text-xs text-fg-2 tabular-nums">{link.count}</span>
				{/if}
			</a>
		{/each}
	</nav>

	{#if user}
		<div class="mx-3 mt-6 mb-3.5 h-px bg-line"></div>
		<div class="flex items-center justify-between px-3 pb-1.5">
			<span class="text-eyebrow text-fg-2">Tus playlists</span>
			{#if visiblePlaylists.length > 0}
				<IconButton
					label="Crear playlist"
					plain
					size="xs"
					onclick={() => createPlaylistModal.show()}
				>
					<Plus class="size-icon-sm text-fg-2" />
				</IconButton>
			{/if}
		</div>

		<div class="flex min-h-0 flex-1 flex-col gap-px overflow-y-auto pb-4.5">
			{#each visiblePlaylists as playlist (playlist.id)}
				{@const active = page.url.pathname === `/playlists/${playlist.id}`}
				<ListRow
					href="/playlists/{playlist.id}"
					{active}
					size="xs"
					title={playlist.name}
					coverSrc={playlistCover(playlist, 'small')}
					trackIds={playlist.coverTrackIds}
					class="px-3 py-2"
				/>
			{:else}
				<button
					type="button"
					onclick={() => createPlaylistModal.show()}
					class="flex items-center gap-2 rounded-control px-3 py-2 text-xs text-fg-2 transition-colors hover:bg-hover"
				>
					<Plus class="size-icon-xs" />
					Crear tu primera playlist
				</button>
			{/each}
		</div>
	{/if}
</aside>
