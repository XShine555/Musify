<script lang="ts">
	import { page } from '$app/state';
	import { isSectionActive } from '$lib/navigation.svelte';
	import type { SessionUser } from '$lib/types';
	import Home from '@lucide/svelte/icons/house';
	import Folder from '@lucide/svelte/icons/folder';
	import Disc from '@lucide/svelte/icons/disc-2';
	import Music from '@lucide/svelte/icons/music';
	import Upload from '@lucide/svelte/icons/upload';

	interface Props {
		user: SessionUser | null;
	}

	let { user }: Props = $props();

	let navBox: HTMLElement | undefined = $state();
	let pillTop = $state(0);
	let pillHeight = $state(0);
	let pillVisible = $state(false);
	let animate = $state(false);

	const mainLinks = $derived([
		{ href: '/', label: 'Inicio', icon: Home },
		...(user ? [{ href: '/playlists', label: 'Listas', icon: Music }] : [])
	]);

	const secondaryLinks = $derived(
		user
			? [
					{ href: '/albums', label: 'Mis álbumes', icon: Disc },
					{ href: '/library', label: 'Canciones subidas', icon: Folder },
					{ href: '/upload', label: 'Subir música', icon: Upload }
				]
			: []
	);

	function isActive(href: string) {
		return isSectionActive(href, page.url.pathname, page.data.section);
	}

	$effect(() => {
		void page.url.pathname;
		const box = navBox;
		if (!box) return;
		const active = box.querySelector<HTMLElement>('[data-active="true"]');
		if (!active) {
			pillVisible = false;
			return;
		}
		const boxRect = box.getBoundingClientRect();
		const rect = active.getBoundingClientRect();
		pillTop = rect.top - boxRect.top;
		pillHeight = rect.height;
		pillVisible = true;
		if (!animate) requestAnimationFrame(() => (animate = true));
	});
</script>

{#snippet navLink(link: { href: string; label: string; icon: typeof Home })}
	{@const active = isActive(link.href)}
	<a
		href={link.href}
		data-active={active}
		class="relative flex items-center gap-3.5 rounded-control px-4 py-2.5 text-sm transition-colors duration-100 {active
			? 'text-fg'
			: 'text-fg-2 hover:bg-hover hover:text-fg'}"
	>
		<link.icon class="h-5 w-5 shrink-0 {active ? 'text-accent-soft' : ''}" />
		{link.label}
	</a>
{/snippet}

{#snippet groupLabel(label: string)}
	<p class="mb-4 px-4 text-sm font-semibold tracking-[0.12em] text-fg-2 uppercase">
		{label}
	</p>
{/snippet}

<aside
	class="sticky top-0 hidden h-screen shrink-0 flex-col border-r border-line bg-bg md:flex"
	style="width:var(--mf-sidebar-w)"
>
	<div
		class="relative flex flex-1 flex-col gap-6 overflow-y-auto px-4.5 py-6 md:pt-8"
		bind:this={navBox}
	>
		<div
			class="pointer-events-none absolute inset-x-4.5 z-0 overflow-hidden rounded-control border border-line bg-surface {animate
				? 'transition-[top,height] duration-200 ease-in-out'
				: ''}"
			style="top:{pillTop}px; height:{pillHeight}px; opacity:{pillVisible ? 1 : 0}"
		>
			<span class="absolute inset-y-0 left-0 w-0.75 rounded-r-full bg-accent-soft"></span>
		</div>

		<div>
			{@render groupLabel('Navegación')}
			<nav class="relative z-10 flex flex-col gap-2">
				{#each mainLinks as link (link.href)}
					{@render navLink(link)}
				{/each}
			</nav>
		</div>

		{#if secondaryLinks.length > 0}
			<div>
				{@render groupLabel('Tu música')}
				<nav class="relative z-10 flex flex-col gap-1">
					{#each secondaryLinks as link (link.href)}
						{@render navLink(link)}
					{/each}
				</nav>
			</div>
		{/if}
	</div>
</aside>
