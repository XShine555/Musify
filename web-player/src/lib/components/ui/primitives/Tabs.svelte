<script lang="ts">
	interface Tab {
		href: string;
		label: string;
		active: boolean;
	}

	interface Props {
		label: string;
		tabs: Tab[];
		class?: string;
	}

	let { label, tabs, class: klass = '' }: Props = $props();

	let links = $state<Record<string, HTMLAnchorElement | undefined>>({});
	let indicator = $state({ x: 0, width: 0 });
	let measured = $state(false);
	let animated = $state(false);

	const activeHref = $derived(tabs.find((tab) => tab.active)?.href);

	$effect(() => {
		const link = activeHref ? links[activeHref] : undefined;
		if (!link) return;
		indicator = { x: link.offsetLeft, width: link.offsetWidth };
		measured = true;
		if (!animated) requestAnimationFrame(() => (animated = true));
	});
</script>

<nav
	aria-label={label}
	class="relative inline-flex max-w-full rounded-full bg-surface-2 p-1 {klass}"
>
	{#if measured}
		<div
			class="absolute inset-y-1 left-0 rounded-full bg-fg ease-snappy {animated
				? 'transition-[transform,width] duration-200'
				: ''}"
			style="width:{indicator.width}px; transform:translateX({indicator.x}px)"
		></div>
	{/if}
	{#each tabs as tab (tab.href)}
		<a
			bind:this={links[tab.href]}
			href={tab.href}
			aria-current={tab.active ? 'page' : undefined}
			class="relative z-(--z-raised) rounded-full px-4 py-1.5 text-xs font-medium focus-ring transition-colors {tab.active
				? 'text-ink'
				: 'text-fg-2 hover:text-fg'}"
		>
			{tab.label}
		</a>
	{/each}
</nav>
