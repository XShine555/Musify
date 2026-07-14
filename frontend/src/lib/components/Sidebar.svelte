<script lang="ts">
	import { page } from '$app/state';
	import type { SessionUser } from '$lib/types';
	import Home from '@lucide/svelte/icons/house';
	import Search from '@lucide/svelte/icons/search';
	import Library from '@lucide/svelte/icons/library-big';
	import ListMusic from '@lucide/svelte/icons/list-music';
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
		{ href: '/playlists', label: 'Mis listas', icon: ListMusic }
	];

	const secondaryLinks = [
		{ href: '/library', label: 'Tus canciones subidas', icon: Library },
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
		class="relative flex items-center gap-3 rounded-[10px] px-3 py-2.5 text-[14px] font-semibold transition-colors duration-100 {active
			? 'text-[var(--mf-text)]'
			: 'text-[var(--mf-text-2)] hover:bg-white/5 hover:text-[var(--mf-text)]'}"
	>
		<link.icon
			class="h-[18px] w-[18px] flex-shrink-0"
			strokeWidth={2}
			style={active ? 'color:var(--mf-accent)' : ''}
		/>
		{link.label}
	</a>
{/snippet}

<aside
	class="sticky top-0 flex h-screen w-[264px] flex-shrink-0 flex-col gap-7 border-r border-[var(--mf-border)] px-[18px] py-[26px]"
>
	<a href="/" class="px-3 font-display text-xl font-extrabold tracking-tight">Musify</a>

	<div class="relative flex flex-col gap-7" bind:this={navBox}>
		<div
			class="pointer-events-none absolute inset-x-0 z-0 overflow-hidden rounded-[10px] bg-white/10 {animate
				? 'transition-[top,height] duration-200 ease-[cubic-bezier(0.4,0,0.2,1)]'
				: ''}"
			style="top:{pillTop}px; height:{pillHeight}px; opacity:{pillVisible ? 1 : 0}"
		>
			<span class="absolute inset-y-0 left-0 w-[3px] rounded-r-full bg-[var(--mf-accent)]"></span>
		</div>

		<nav class="relative z-10 flex flex-col gap-1">
			{#each mainLinks as link (link.href)}
				{@render navLink(link)}
			{/each}
		</nav>

		<div class="mx-2 border-t border-[var(--mf-border)]"></div>

		<nav class="relative z-10 flex flex-col gap-1">
			{#each secondaryLinks as link (link.href)}
				{@render navLink(link)}
			{/each}
		</nav>
	</div>

	<div class="relative mt-auto" bind:this={menuRef}>
		{#if menuOpen}
			<div
				class="absolute bottom-full left-0 mb-2 w-full overflow-hidden rounded-xl border border-[var(--mf-border)] bg-[var(--mf-elevated)] py-1.5 shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)]"
			>
				<form method="POST" action="/logout" data-sveltekit-reload>
					<button
						type="submit"
						class="flex w-full items-center gap-3 px-3 py-2.5 text-sm font-semibold text-[var(--mf-text-2)] transition hover:bg-white/5"
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
			class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-left transition hover:bg-white/5"
		>
			{#if user.picture}
				<img src={user.picture} alt="" class="h-9 w-9 rounded-full object-cover" />
			{:else}
				<span
					class="grid h-9 w-9 flex-shrink-0 place-items-center rounded-full bg-[var(--mf-surface-2)] text-xs font-semibold uppercase"
				>
					{user.name.charAt(0)}
				</span>
			{/if}
			<span class="truncate text-[13px] font-semibold leading-[1.35]">{user.name}</span>
		</button>
	</div>
</aside>

<svelte:window onclick={onDocumentClick} />
