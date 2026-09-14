<script lang="ts">
	import type { Snippet } from 'svelte';
	import Check from '@lucide/svelte/icons/check';

	interface Props {
		checked?: boolean;
		name?: string;
		class?: string;
		children: Snippet;
	}

	let { checked = $bindable(false), name, class: klass = '', children }: Props = $props();
</script>

<label class="flex items-center gap-3 text-sm text-fg-2 {klass}">
	<span class="relative inline-grid h-5 w-5 shrink-0 place-items-center">
		<input
			type="checkbox"
			{name}
			bind:checked
			class="peer h-5 w-5 cursor-pointer appearance-none rounded border border-line bg-surface transition checked:border-accent-soft checked:bg-accent-soft focus-visible:border-accent/60 focus-visible:outline-none"
		/>
		<Check
			class="pointer-events-none absolute h-3.5 w-3.5 text-ink opacity-0 transition peer-checked:opacity-100"
			strokeWidth={3}
		/>
	</span>
	<span class="cursor-pointer">{@render children()}</span>
</label>
