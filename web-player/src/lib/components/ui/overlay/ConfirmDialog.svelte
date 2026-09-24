<script lang="ts">
	import { enhance } from '$app/forms';
	import type { SubmitFunction } from '@sveltejs/kit';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
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
		submit?: SubmitFunction;
		error?: string;
	}

	let {
		open,
		onClose,
		title,
		description,
		confirmLabel,
		action,
		fields = {},
		submit,
		error
	}: Props = $props();
</script>

{#snippet body()}
	{#each Object.entries(fields) as [name, value] (name)}
		<input type="hidden" {name} {value} />
	{/each}
	<Button type="submit" variant="danger">{confirmLabel}</Button>
{/snippet}

<Dialog {open} {onClose} {title} {description}>
	{#if error}
		<Alert tone="danger" class="mt-4">{error}</Alert>
	{/if}
	{#snippet actions()}
		<Button variant="secondary" onclick={onClose}>Cancelar</Button>
		{#if submit}
			<form method="POST" {action} use:enhance={submit}>
				{@render body()}
			</form>
		{:else}
			<form method="POST" {action}>
				{@render body()}
			</form>
		{/if}
	{/snippet}
</Dialog>
