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
		<span class="pointer-events-none absolute inset-y-0 left-4 grid place-items-center text-fg-2">
			<Icon class="size-icon-lg" />
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
		class="field-control {Icon ? 'pr-4 pl-11' : 'px-4'} {klass}"
	/>
</div>
