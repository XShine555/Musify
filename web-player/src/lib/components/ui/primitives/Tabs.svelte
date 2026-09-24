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
</script>

<nav aria-label={label} class="flex gap-2 {klass}">
	{#each tabs as tab (tab.href)}
		<a
			href={tab.href}
			aria-current={tab.active ? 'page' : undefined}
			class="rounded-full px-4 py-1.5 text-xs font-medium focus-ring transition-colors {tab.active
				? 'bg-fg text-ink'
				: 'bg-surface-2 text-fg-2 hover:text-fg'}"
		>
			{tab.label}
		</a>
	{/each}
</nav>
