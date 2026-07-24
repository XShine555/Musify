<script lang="ts">
	import { page } from '$app/state';
	import { isSectionActive } from '$lib/navigation.svelte';
	import Home from '@lucide/svelte/icons/house';
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import Folder from '@lucide/svelte/icons/folder';

	const links = [
		{ href: '/', label: 'Inicio', icon: Home },
		{ href: '/explore', label: 'Buscar', icon: Search },
		{ href: '/playlists', label: 'Listas', icon: Music },
		{ href: '/library', label: 'Subidas', icon: Folder }
	];

	function isActive(href: string) {
		return isSectionActive(href, page.url.pathname, page.data.section);
	}
</script>

<nav
	class="fixed inset-x-0 bottom-0 z-50 border-t border-line bg-bg/95 backdrop-blur-md md:hidden"
	style="padding-bottom:var(--mf-safe-b)"
>
	<ul class="grid h-[var(--mf-nav-h)] grid-cols-4">
		{#each links as link (link.href)}
			{@const active = isActive(link.href)}
			<li>
				<a
					href={link.href}
					aria-current={active ? 'page' : undefined}
					class="flex h-full flex-col items-center justify-center gap-1 transition-colors {active
						? 'text-fg'
						: 'text-fg-3'}"
				>
					<link.icon class="h-[22px] w-[22px] {active ? 'text-accent-soft' : ''}" />
					<span class="text-[11px] leading-none font-medium">{link.label}</span>
				</a>
			</li>
		{/each}
	</ul>
</nav>
