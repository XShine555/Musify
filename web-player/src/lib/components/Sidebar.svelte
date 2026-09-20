<script lang="ts">
	import { page } from '$app/state';
	import { isSectionActive } from '$lib/navigation.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { createPlaylistModal } from '$lib/playlists.svelte';
	import type { SessionUser } from '$lib/types';
	import ListRow from './ui/ListRow.svelte';
	import IconButton from './ui/IconButton.svelte';
	import Home from '@lucide/svelte/icons/house';
	import Compass from '@lucide/svelte/icons/compass';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Heart from '@lucide/svelte/icons/heart';
	import Disc from '@lucide/svelte/icons/disc-2';
	import Folder from '@lucide/svelte/icons/folder';
	import Upload from '@lucide/svelte/icons/upload';
	import Plus from '@lucide/svelte/icons/plus';

	interface SidebarPlaylist {
		id: string;
		name: string;
		coverTrackIds: (string | number)[];
		updatedAt: string;
	}

	interface Props {
		user: SessionUser | null;
		playlists: SidebarPlaylist[];
	}

	let { user, playlists }: Props = $props();

	const navLinks = $derived(
		user
			? [
					{ href: '/', label: 'Inicio', icon: Home },
					{ href: '/explore', label: 'Descubrir', icon: Compass },
					{ href: '/playlists', label: 'Playlists', icon: ListMusic, count: playlists.length },
					{ href: '/liked', label: 'Me gusta', icon: Heart, count: liked.count },
					{ href: '/albums', label: 'Mis álbumes', icon: Disc },
					{ href: '/library', label: 'Canciones subidas', icon: Folder },
					{ href: '/upload', label: 'Subir música', icon: Upload }
				]
			: [{ href: '/explore', label: 'Descubrir', icon: Compass }]
	);

	function isActive(href: string) {
		return isSectionActive(href, page.url.pathname, page.data.section);
	}
</script>

<aside
	class="hidden shrink-0 flex-col border-r border-hairline bg-bg p-5 transition-[background] duration-500 ease-out lg:flex"
	style="width:var(--mf-sidebar-w)"
>
	<a href="/" class="px-3 pb-3 font-display text-lg font-semibold tracking-tight text-fg">Musify</a>

	<nav class="flex flex-col gap-1">
		{#each navLinks as link (link.href)}
			{@const active = isActive(link.href)}
			<a
				href={link.href}
				aria-current={active ? 'page' : undefined}
				class="flex items-center gap-3 rounded-control px-3 py-2 text-sm transition-colors duration-150 {active
					? 'bg-surface-2 text-fg'
					: 'text-fg-2 hover:bg-hover hover:text-fg'}"
			>
				<link.icon class="h-4.5 w-4.5 shrink-0" strokeWidth={1.5} />
				<span class="flex-1 truncate">{link.label}</span>
				{#if link.count}
					<span class="text-xs text-muted tabular-nums">{link.count}</span>
				{/if}
			</a>
		{/each}
	</nav>

	{#if user}
		<div class="mx-3 mt-6 mb-3.5 h-px bg-line"></div>
		<div class="flex items-center justify-between px-3 pb-1.5">
			<span class="text-eyebrow text-muted">Tus playlists</span>
			{#if playlists.length > 0}
				<IconButton
					label="Crear playlist"
					tone="plain"
					size="xs"
					onclick={() => createPlaylistModal.show()}
				>
					<Plus class="size-icon-sm" strokeWidth={1.25} />
				</IconButton>
			{/if}
		</div>

		<div class="flex flex-1 flex-col gap-px overflow-y-auto pb-4.5">
			{#each playlists as playlist (playlist.id)}
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
					class="px-3 py-1.75"
				/>
			{:else}
				<button
					type="button"
					onclick={() => createPlaylistModal.show()}
					class="flex items-center gap-2 rounded-control px-3 py-2 text-xs text-muted transition-colors hover:bg-hover hover:text-fg-2"
				>
					<Plus class="h-3.5 w-3.5" strokeWidth={1.8} />
					Crear tu primera lista
				</button>
			{/each}
		</div>
	{/if}
</aside>
