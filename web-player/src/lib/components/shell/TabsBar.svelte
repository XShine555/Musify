<script lang="ts">
	import { appNavLinks, isNavActive } from './navLinks';
	import type { SessionUser } from '$lib/types';

	interface Props {
		user: SessionUser | null;
		class?: string;
	}

	let { user, class: klass = '' }: Props = $props();

	const links = $derived(appNavLinks(user).filter((link) => link.primary));
</script>

<nav
	class="pointer-events-auto flex items-center justify-around px-4 py-2 glass-bar lg:hidden {klass}"
	style="padding-bottom:calc(0.4375rem + var(--mf-safe-b))"
>
	{#each links as link (link.href)}
		{@const active = isNavActive(link.href)}
		<a
			href={link.href}
			aria-current={active ? 'page' : undefined}
			class="flex flex-col items-center gap-1 rounded-control px-2.5 py-1.5 {active
				? 'text-fg'
				: 'text-fg-2'}"
		>
			<link.icon class="size-icon-sm" />
			<span class="text-xs font-medium">{link.label}</span>
		</a>
	{/each}
</nav>
