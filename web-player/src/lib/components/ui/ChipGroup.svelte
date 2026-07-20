<script lang="ts" module>
	import type { LucideIcon } from '@lucide/svelte';

	export interface ChipOption {
		value: string;
		label: string;
		icon?: LucideIcon;
		disabled?: boolean;
		hint?: string;
	}
</script>

<script lang="ts">
	import Check from '@lucide/svelte/icons/check';

	interface Props {
		options: ChipOption[];
		selected: string[];
		label: string;
		onChange: (selected: string[]) => void;
		class?: string;
	}

	let { options, selected, label, onChange, class: klass = '' }: Props = $props();

	function toggle(value: string) {
		onChange(selected.includes(value) ? selected.filter((v) => v !== value) : [...selected, value]);
	}
</script>

<div role="group" aria-label={label} class="flex flex-wrap items-center gap-2 {klass}">
	{#each options as option (option.value)}
		{@const active = selected.includes(option.value)}
		<label
			title={option.hint}
			class="inline-flex cursor-pointer items-center gap-1.5 rounded-full border px-3.5 py-1.5 text-sm font-semibold transition select-none
				{active
				? 'border-transparent bg-accent text-on-accent'
				: 'border-line bg-surface text-fg-2 hover:bg-surface-hover'}
				{option.disabled ? 'pointer-events-none opacity-45' : ''}"
		>
			<input
				type="checkbox"
				class="sr-only"
				checked={active}
				disabled={option.disabled}
				onchange={() => toggle(option.value)}
			/>
			{#if active}
				<Check class="h-3.5 w-3.5" strokeWidth={3} />
			{:else if option.icon}
				<option.icon class="h-3.5 w-3.5" strokeWidth={2} />
			{/if}
			{option.label}
		</label>
	{/each}
</div>
