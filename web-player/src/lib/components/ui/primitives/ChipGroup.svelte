<script lang="ts" generics="T extends string">
	import Chip from './Chip.svelte';

	interface Option {
		value: T;
		label: string;
		count?: number;
	}

	interface Props {
		options: readonly Option[];
		value: T;
		label: string;
		class?: string;
	}

	let { options, value = $bindable(), label, class: klass = '' }: Props = $props();
</script>

<div role="group" aria-label={label} class="flex flex-wrap gap-2 {klass}">
	{#each options as option (option.value)}
		<Chip
			selected={value === option.value}
			count={option.count}
			onclick={() => (value = option.value)}
		>
			{option.label}
		</Chip>
	{/each}
</div>
