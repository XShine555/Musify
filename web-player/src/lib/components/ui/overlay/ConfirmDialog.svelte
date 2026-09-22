<script lang="ts">
	import Dialog from './Dialog.svelte';
	import Button from '../primitives/Button.svelte';

	interface Props {
		open: boolean;
		onClose: () => void;
		title: string;
		description: string;
		confirmLabel: string;
		action: string;
	}

	let { open, onClose, title, description, confirmLabel, action }: Props = $props();
</script>

<Dialog {open} {onClose} {title} {description}>
	{#snippet actions()}
		<Button variant="secondary" onclick={onClose}>Cancelar</Button>
		<form method="POST" {action}>
			<Button type="submit" variant="danger">{confirmLabel}</Button>
		</form>
	{/snippet}
</Dialog>
