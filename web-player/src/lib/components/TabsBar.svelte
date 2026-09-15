<script lang="ts">
	import { page } from '$app/state';
	import { isSectionActive } from '$lib/navigation.svelte';
	import type { SessionUser } from '$lib/types';
	import Home from '@lucide/svelte/icons/house';
	import Compass from '@lucide/svelte/icons/compass';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Heart from '@lucide/svelte/icons/heart';

	interface Props {
		user: SessionUser | null;
		class?: string;
	}

	let { user, class: klass = '' }: Props = $props();

	const links = $derived(
		user
			? [
					{ href: '/', label: 'Inicio', icon: Home },
					{ href: '/explore', label: 'Descubrir', icon: Compass },
					{ href: '/playlists', label: 'Playlists', icon: ListMusic },
					{ href: '/liked', label: 'Me gusta', icon: Heart }
				]
			: [{ href: '/explore', label: 'Descubrir', icon: Compass }]
	);

	function isActive(href: string) {
		return isSectionActive(href, page.url.pathname, page.data.section);
	}
</script>

<nav
	class="pointer-events-auto flex items-center justify-around rounded-[18px] border border-hairline bg-[image:var(--mf-bar-bg)] p-1.75 backdrop-blur-[22px] transition-[background] duration-[600ms] lg:hidden {klass}"
	style="padding-bottom:calc(0.4375rem + var(--mf-safe-b))"
>
	{#each links as link (link.href)}
		{@const active = isActive(link.href)}
		<a
			href={link.href}
			aria-current={active ? 'page' : undefined}
			class="flex flex-col items-center gap-1.25 rounded-xl px-2.5 py-1.5 {active
				? 'text-fg'
				: 'text-fg-2'}"
		>
			<link.icon class="h-[17px] w-[17px]" strokeWidth={1.6} />
			<span class="text-xs font-medium">{link.label}</span>
		</a>
	{/each}
</nav>
