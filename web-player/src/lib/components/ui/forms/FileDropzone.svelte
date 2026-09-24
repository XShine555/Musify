<script lang="ts">
	import UploadCloud from '@lucide/svelte/icons/cloud-upload';
	import Button from '$lib/components/ui/primitives/Button.svelte';

	interface Props {
		accept: string;
		title: string;
		hint?: string;
		buttonLabel?: string;
		inputLabel?: string;
		multiple?: boolean;
		onfiles: (files: File[]) => void;
		readDrop?: (transfer: DataTransfer) => Promise<File[]>;
		class?: string;
	}

	let {
		accept,
		title,
		hint,
		buttonLabel = 'Elegir archivos',
		inputLabel = buttonLabel,
		multiple = true,
		onfiles,
		readDrop = async (transfer) => Array.from(transfer.files),
		class: klass = ''
	}: Props = $props();

	let depth = $state(0);
	let input = $state<HTMLInputElement>();

	const dragging = $derived(depth > 0);

	function hasFiles(event: DragEvent): boolean {
		return event.dataTransfer?.types.includes('Files') ?? false;
	}

	function onEnter(event: DragEvent) {
		if (!hasFiles(event)) return;
		event.preventDefault();
		depth += 1;
	}

	function onOver(event: DragEvent) {
		if (hasFiles(event)) event.preventDefault();
	}

	function onLeave(event: DragEvent) {
		if (!hasFiles(event)) return;
		depth = Math.max(0, depth - 1);
	}

	async function onDrop(event: DragEvent) {
		event.preventDefault();
		depth = 0;
		if (!event.dataTransfer) return;
		onfiles(await readDrop(event.dataTransfer));
	}

	function onPick(event: Event) {
		const target = event.currentTarget as HTMLInputElement;
		onfiles(Array.from(target.files ?? []));
		target.value = '';
	}
</script>

<div
	role="presentation"
	ondragenter={onEnter}
	ondragover={onOver}
	ondragleave={onLeave}
	ondrop={onDrop}
	class="flex min-h-64 flex-col items-center justify-center rounded-panel border border-dashed p-6 text-center transition sm:p-10 {dragging
		? 'border-accent/60 bg-accent-tint'
		: 'border-line-strong bg-surface'} {klass}"
>
	<span class="grid size-14 place-items-center rounded-full bg-surface-2 text-fg-2">
		<UploadCloud class="size-icon-xl" />
	</span>
	<h2 class="mt-5 font-display text-xl font-semibold tracking-display text-fg">{title}</h2>
	{#if hint}
		<p class="mt-2 text-xs text-fg-2">{hint}</p>
	{/if}
	<Button class="mt-6" onclick={() => input?.click()}>{buttonLabel}</Button>
	<input
		bind:this={input}
		type="file"
		{multiple}
		{accept}
		onchange={onPick}
		class="hidden"
		aria-label={inputLabel}
		tabindex="-1"
	/>
</div>
