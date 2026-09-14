<script lang="ts">
	import type { LucideIcon } from '@lucide/svelte';

	interface Props {
		value?: string;
		id?: string;
		name?: string;
		type?: 'text' | 'search' | 'number';
		min?: number;
		max?: number;
		placeholder?: string;
		maxlength?: number;
		required?: boolean;
		autocomplete?: 'on' | 'off';
		icon?: LucideIcon;
		oninput?: (event: Event) => void;
		class?: string;
	}

	let {
		value = $bindable(''),
		id,
		name,
		type = 'text',
		min,
		max,
		placeholder,
		maxlength,
		required = false,
		autocomplete = 'off',
		icon: Icon,
		oninput,
		class: klass = ''
	}: Props = $props();
</script>

<div class="relative w-full">
	{#if Icon}
		<span class="pointer-events-none absolute inset-y-0 left-4 grid place-items-center text-muted">
			<Icon class="h-5 w-5" />
		</span>
	{/if}
	<input
		{id}
		{name}
		{type}
		{min}
		{max}
		{placeholder}
		{maxlength}
		{required}
		{autocomplete}
		{oninput}
		bind:value
		class="w-full rounded-control bg-surface py-3 text-sm text-fg transition placeholder:text-muted focus:bg-surface-2 focus:outline-none {Icon
			? 'pr-4 pl-11'
			: 'px-4'} {klass}"
	/>
</div>
