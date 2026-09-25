<script lang="ts" module>
	export interface Tag {
		id: string;
		label: string;
	}
</script>

<script lang="ts">
	import X from '@lucide/svelte/icons/x';

	interface Props {
		tags: Tag[];
		value?: string;
		label: string;
		id?: string;
		placeholder?: string;
		disabled?: boolean;
		controls?: string;
		onadd?: (text: string) => void;
		onremove: (id: string) => void;
		oninput?: () => void;
		class?: string;
	}

	let {
		tags,
		value = $bindable(''),
		label,
		id,
		placeholder,
		disabled = false,
		controls,
		onadd,
		onremove,
		oninput,
		class: klass = ''
	}: Props = $props();

	let input: HTMLInputElement | undefined = $state();

	function commit() {
		const text = value.trim();
		if (!text) return;
		onadd?.(text);
	}

	function onkeydown(event: KeyboardEvent) {
		if (event.key === 'Enter' || event.key === ',') {
			event.preventDefault();
			commit();
		} else if (event.key === 'Backspace' && value === '' && tags.length > 0) {
			onremove(tags[tags.length - 1].id);
		}
	}

	function remove(tagId: string) {
		onremove(tagId);
		input?.focus();
	}
</script>

<div
	role="group"
	aria-label={label}
	class="flex min-h-11 flex-wrap items-center gap-2 rounded-control bg-surface-2 px-3 py-2 transition focus-within:bg-surface-hover {disabled
		? 'opacity-40'
		: ''} {klass}"
>
	<ul class="contents">
		{#each tags as tag (tag.id)}
			<li class="flex items-center gap-1 rounded-full bg-btn py-1 pr-1 pl-3 text-xs text-fg">
				<span class="max-w-40 truncate">{tag.label}</span>
				<button
					type="button"
					{disabled}
					aria-label="Quitar {tag.label}"
					onclick={() => remove(tag.id)}
					class="grid size-5 shrink-0 place-items-center rounded-full text-fg-2 focus-ring transition-colors hover:bg-btn-hover hover:text-fg"
				>
					<X class="size-icon-xs" />
				</button>
			</li>
		{/each}
	</ul>
	<input
		bind:this={input}
		bind:value
		{id}
		{placeholder}
		{disabled}
		{oninput}
		{onkeydown}
		type="text"
		autocomplete="off"
		aria-label={label}
		aria-controls={controls}
		class="min-w-32 flex-1 bg-transparent text-sm text-fg placeholder:text-fg-2 focus:outline-none"
	/>
</div>
