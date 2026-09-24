<script lang="ts">
	import { page } from '$app/state';
	import { isSectionActive, appNavLinks } from '$lib/state/navigation.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { createPlaylistModal } from '$lib/state/playlists.svelte';
	import type { SessionUser } from '$lib/types';
	import ListRow from '../ui/media/ListRow.svelte';
	import IconButton from '../ui/primitives/IconButton.svelte';
	import Plus from '@lucide/svelte/icons/plus';
	import Logo from '../ui/primitives/Logo.svelte';

	const SIDEBAR_PLAYLISTS_LIMIT = 8;

	interface SidebarPlaylist {
		id: string;
		name: string;
		coverTrackIds: (string | number)[];
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

	function isActive(href: string) {
		return isSectionActive(href, page.url.pathname, page.data.section);
	}
</script>

<aside
	class="hidden min-h-0 shrink-0 flex-col border-r border-hairline p-5 lg:flex"
	style="width:var(--mf-sidebar-w)"
>
	<a href="/" class="mb-4">
		<Logo size="sm" />
	</a>

	<nav class="flex flex-col gap-1">
		{#each navLinks as link (link.href)}
			{@const active = isActive(link.href)}
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
					tone="plain"
					size="xs"
					onclick={() => createPlaylistModal.show()}
				>
					<Plus class="size-icon-sm text-fg-2" />
				</IconButton>
			{/if}
		</div>

		<div class="flex min-h-0 flex-1 flex-col gap-px overflow-y-auto pb-icon-md">
			{#each visiblePlaylists as playlist (playlist.id)}
				{@const active = page.url.pathname === `/playlists/${playlist.id}`}
				<ListRow
					href="/playlists/{playlist.id}"
					{active}
					size="xs"
					title={playlist.name}
					coverSrc="/api/playlists/{playlist.id}/cover?size=small&v={encodeURIComponent(
						playlist.updatedAt
					)}"
					trackIds={playlist.coverTrackIds}
					class="px-3 py-2"
				/>
			{:else}
				<button
					type="button"
					onclick={() => createPlaylistModal.show()}
					class="flex items-center gap-2 rounded-control px-3 py-2 text-xs text-fg-2 transition-colors hover:bg-hover hover:text-fg-2"
				>
					<Plus class="size-3.5" />
					Crear tu primera lista
				</button>
			{/each}
		</div>
	{/if}
</aside>
