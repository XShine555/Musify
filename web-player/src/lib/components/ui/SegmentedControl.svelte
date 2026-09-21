<script lang="ts" generics="T extends string">
	interface Option<T> {
		value: T;
		label: string;
	}

	interface Props {
		options: Option<T>[];
		value: T;
	}

	let { options, value = $bindable() }: Props = $props();

	const index = $derived(options.findIndex((option) => option.value === value));
</script>

<div class="relative flex shrink-0 rounded-full bg-surface-2 p-1">
	<div
		class="absolute inset-y-1 left-1 rounded-full bg-fg transition-transform duration-300 ease-out"
		style="width:calc((100% - 0.5rem) / {options.length}); transform:translateX(calc({index} * 100%))"
	></div>
	{#each options as option (option.value)}
		<button
			type="button"
			onclick={() => (value = option.value)}
			class="relative z-(--z-raised) flex-1 px-4 py-1.5 text-center text-xs font-medium transition-colors {value ===
			option.value
				? 'text-ink'
				: 'text-muted hover:text-fg-2'}"
		>
			{option.label}
		</button>
	{/each}
</div>
