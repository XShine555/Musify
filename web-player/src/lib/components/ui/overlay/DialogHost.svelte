<script lang="ts">
	import Dialog from './Dialog.svelte';
	import Button from '../primitives/Button.svelte';
	import { dialog, type DialogAction, type DialogOptions } from '$lib/state/dialog.svelte';

	// Keep the last dialog's content so the exit transition doesn't render an empty panel.
	let last = $state<DialogOptions | null>(null);
	$effect.pre(() => {
		if (dialog.current) last = dialog.current;
	});
	const current = $derived(dialog.current ?? last);
	const buttons: DialogAction[] = $derived(
		current?.actions?.length ? current.actions : [{ label: 'Entendido' }]
	);
</script>

<Dialog
	open={dialog.current !== null}
	onClose={() => dialog.close()}
	title={current?.title ?? ''}
	description={current?.description}
	tone={current?.tone}
>
	{#snippet actions()}
		{#each buttons as action, i (action.label)}
			<Button
				variant={action.variant ?? (i === buttons.length - 1 ? 'primary' : 'secondary')}
				onclick={() => dialog.run(action)}
			>
				{action.label}
			</Button>
		{/each}
	{/snippet}
</Dialog>
