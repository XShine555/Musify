<script lang="ts">
	import type { Snippet } from 'svelte';
	import CircleAlert from '@lucide/svelte/icons/circle-alert';
	import Modal from './Modal.svelte';

	interface Props {
		open: boolean;
		onClose: () => void;
		title: string;
		description?: string;
		tone?: 'neutral' | 'danger';
		children?: Snippet;
		actions?: Snippet;
	}

	let { open, onClose, title, description, tone = 'neutral', children, actions }: Props = $props();
</script>

<Modal
	{open}
	{onClose}
	maxWidth="max-w-sm"
	panelClass={tone === 'danger' ? 'glass-panel-danger' : ''}
>
	{#if tone === 'danger'}
		<span class="mb-4 grid size-10 place-items-center rounded-full bg-danger/12 text-danger-fg">
			<CircleAlert class="size-icon-md" />
		</span>
	{/if}
	<h2 class="text-display-3 text-fg">{title}</h2>
	{#if description}
		<p class="mt-2.5 text-body text-fg-2">{description}</p>
	{/if}
	{@render children?.()}
	{#if actions}
		<div class="mt-6 flex flex-wrap items-center justify-end gap-3">
			{@render actions()}
		</div>
	{/if}
</Modal>
