<script lang="ts">
	import Modal from './Modal.svelte';
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

<Modal {open} {onClose} maxWidth="max-w-sm">
	<h2 class="text-display-3 text-fg">{title}</h2>
	<p class="mt-2.5 text-body text-fg-2">{description}</p>
	<div class="mt-6 flex items-center justify-end gap-3">
		<Button variant="secondary" onclick={onClose}>Cancelar</Button>
		<form method="POST" {action}>
			<Button type="submit" variant="danger">{confirmLabel}</Button>
		</form>
	</div>
</Modal>
