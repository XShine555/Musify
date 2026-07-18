<script lang="ts">
	import { page } from '$app/state';
	import type { SessionUser } from '$lib/types';
	import Home from '@lucide/svelte/icons/house';
	import Search from '@lucide/svelte/icons/search';
	import Folder from '@lucide/svelte/icons/folder';
	import Music from '@lucide/svelte/icons/music';
	import Upload from '@lucide/svelte/icons/upload';
	import LogOut from '@lucide/svelte/icons/log-out';

	interface Props {
		user: SessionUser;
	}

	let { user }: Props = $props();

	let menuOpen = $state(false);
	let menuRef: HTMLDivElement | undefined = $state();

	let navBox: HTMLElement | undefined = $state();
	let pillTop = $state(0);
	let pillHeight = $state(0);
	let pillVisible = $state(false);
	let animate = $state(false);

	function onDocumentClick(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) menuOpen = false;
	}

	const mainLinks = [
		{ href: '/', label: 'Inicio', icon: Home },
		{ href: '/explore', label: 'Buscar', icon: Search },
		{ href: '/playlists', label: 'Listas', icon: Music }
	];

	const secondaryLinks = [
		{ href: '/library', label: 'Canciones subidas', icon: Folder },
		{ href: '/upload', label: 'Subir música', icon: Upload }
	];

	function isActive(href: string) {
		return href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href);
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
		class="relative flex items-center gap-4 rounded-control px-4 py-3 transition-colors duration-100 {active
			? 'text-fg'
			: 'text-fg-3 hover:bg-hover hover:text-fg'}"
	>
		<link.icon
			class="h-6 w-6 shrink-0"
			style={active ? 'color:var(--mf-accent-60)' : ''}
		/>
		{link.label}
	</a>
{/snippet}

<aside
	class="sticky top-0 flex h-screen flex-shrink-0 flex-col gap-7 border-r border-line px-[18px] py-[26px]"
	style="width:var(--mf-sidebar-w)"
>
	<a href="/" class="px-3 text-xl font-extrabold tracking-wide">Musify</a>

	<div class="relative flex flex-col gap-7" bind:this={navBox}>
		<div
			class="pointer-events-none absolute inset-x-0 z-0 overflow-hidden rounded-control bg-surface-hover {animate
				? 'transition-[top,height] duration-200 ease-[cubic-bezier(0.4,0,0.2,1)]'
				: ''}"
			style="top:{pillTop}px; height:{pillHeight}px; opacity:{pillVisible ? 1 : 0}"
		>
			<span class="absolute inset-y-0 left-0 w-[3px] rounded-r-full" style="background:var(--mf-accent-60)"></span>
		</div>

		<nav class="relative z-10 flex flex-col gap-1">
			{#each mainLinks as link (link.href)}
				{@render navLink(link)}
			{/each}
		</nav>

		<div class="mx-2 border-t border-line"></div>

		<nav class="relative z-10 flex flex-col gap-1">
			{#each secondaryLinks as link (link.href)}
				{@render navLink(link)}
			{/each}
		</nav>
	</div>

	<div class="relative mt-auto" bind:this={menuRef}>
		{#if menuOpen}
			<div
				class="absolute bottom-full left-0 mb-2 w-full overflow-hidden rounded-panel border border-line bg-elevated py-1.5 shadow-menu"
			>
				<form method="POST" action="/logout" data-sveltekit-reload>
					<button
						type="submit"
						class="flex w-full items-center gap-3 px-3 py-2.5 text-base font-semibold text-fg-2 transition hover:bg-hover"
					>
						<LogOut class="h-[18px] w-[18px]" strokeWidth={2} />
						Salir
					</button>
				</form>
			</div>
		{/if}
		<button
			type="button"
			onclick={() => (menuOpen = !menuOpen)}
			class="flex w-full items-center gap-2.5 rounded-control px-3 py-2.5 text-left transition hover:bg-hover"
		>
			{#if user.picture}
				<img src={user.picture} alt="" class="h-9 w-9 rounded-full object-cover" />
			{:else}
				<span
					class="grid h-9 w-9 flex-shrink-0 place-items-center rounded-full bg-surface-2 text-sm font-semibold uppercase"
				>
					{user.name.charAt(0)}
				</span>
			{/if}
			<span class="truncate text-base font-semibold leading-[1.35]">{user.name}</span>
		</button>
	</div>
</aside>

<svelte:window onclick={onDocumentClick} />
