<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import X from '@lucide/svelte/icons/x';

	interface Props {
		value?: string;
		placeholder?: string;
		label?: string;
		oninput?: () => void;
		onclear?: () => void;
		class?: string;
	}

	let {
		value = $bindable(''),
		placeholder = 'Buscar',
		label = placeholder,
		oninput,
		onclear,
		class: klass = ''
	}: Props = $props();

	let input: HTMLInputElement | undefined = $state();

	function clear() {
		value = '';
		onclear?.();
		input?.focus();
	}
</script>

<label
	class="flex h-9.5 min-w-0 items-center gap-2.5 rounded-control bg-surface-2 px-3.5 transition focus-within:bg-surface-hover {klass}"
>
	<Search class="size-icon-xs shrink-0 text-fg-2" />
	<input
		type="text"
		bind:this={input}
		bind:value
		{oninput}
		{placeholder}
		aria-label={label}
		class="w-full min-w-0 bg-transparent text-xs text-fg placeholder:text-fg-2 focus:outline-none"
	/>
	{#if value}
		<button
			type="button"
			onclick={clear}
			aria-label="Borrar búsqueda"
			class="grid size-5 shrink-0 place-items-center rounded-full text-fg-2 transition hover:text-fg"
		>
			<X class="size-icon-xs" />
		</button>
	{/if}
</label>
