<script lang="ts">
	import { page } from '$app/state';
	import { isSectionActive, appNavLinks } from '$lib/state/navigation.svelte';
	import type { SessionUser } from '$lib/types';

	interface Props {
		user: SessionUser | null;
		class?: string;
	}

	let { user, class: klass = '' }: Props = $props();

	const links = $derived(appNavLinks(user).filter((link) => link.primary));

	function isActive(href: string) {
		return isSectionActive(href, page.url.pathname, page.data.section);
	}
</script>

<nav
	class="pointer-events-auto flex items-center justify-around px-4 py-2 glass-bar lg:hidden {klass}"
	style="padding-bottom:calc(0.4375rem + var(--mf-safe-b))"
>
	{#each links as link (link.href)}
		{@const active = isActive(link.href)}
		<a
			href={link.href}
			aria-current={active ? 'page' : undefined}
			class="flex flex-col items-center gap-1 rounded-control px-2.5 py-1.5 {active
				? 'text-fg'
				: 'text-fg-2'}"
		>
			<link.icon class="size-4" />
			<span class="text-xs font-medium">{link.label}</span>
		</a>
	{/each}
</nav>
