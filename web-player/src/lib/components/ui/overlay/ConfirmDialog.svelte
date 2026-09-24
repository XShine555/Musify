<script lang="ts">
	import Dialog from './Dialog.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';

	interface Props {
		open: boolean;
		onClose: () => void;
		title: string;
		description: string;
		confirmLabel: string;
		action: string;
		fields?: Record<string, string>;
	}

	let { open, onClose, title, description, confirmLabel, action, fields = {} }: Props = $props();
</script>

<Dialog {open} {onClose} {title} {description}>
	{#snippet actions()}
		<Button variant="secondary" onclick={onClose}>Cancelar</Button>
		<form method="POST" {action}>
			{#each Object.entries(fields) as [name, value] (name)}
				<input type="hidden" {name} {value} />
			{/each}
			<Button type="submit" variant="danger">{confirmLabel}</Button>
		</form>
	{/snippet}
</Dialog>
