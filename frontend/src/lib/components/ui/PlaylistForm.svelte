<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import ImageDropzone from './ImageDropzone.svelte';
	import Button from './Button.svelte';
	import Field from './Field.svelte';
	import Input from './Input.svelte';
	import Textarea from './Textarea.svelte';
	import Alert from './Alert.svelte';

	interface Props {
		action: string;
		initialName?: string;
		initialDescription?: string;
		namePlaceholder?: string;
		coverFallbackUrl?: string;
		formMessage?: string;
		submitLabel: string;
		submittingLabel: string;
		onCancel: () => void;
		onSuccess?: () => void;
	}

	let {
		action,
		initialName = '',
		initialDescription = '',
		namePlaceholder = '',
		coverFallbackUrl,
		formMessage,
		submitLabel,
		submittingLabel,
		onCancel,
		onSuccess
	}: Props = $props();

	let name = $state(untrack(() => initialName));
	let description = $state(untrack(() => initialDescription));
	let submitting = $state(false);
</script>

<form
	method="POST"
	{action}
	enctype="multipart/form-data"
	use:enhance={() => {
		submitting = true;
		return async ({ update, result }) => {
			await update({ reset: false });
			submitting = false;
			if (result.type === 'success') onSuccess?.();
		};
	}}
	class="mt-4 w-full max-w-4xl space-y-4"
>
	<div class="flex items-stretch gap-8">
		<Field label="Portada" optional>
			<ImageDropzone name="cover" fallbackUrl={coverFallbackUrl} />
		</Field>

		<div class="flex flex-1 flex-col gap-4">
			<Field label="Nombre" for="playlist-name">
				<Input
					id="playlist-name"
					name="name"
					maxlength={100}
					required
					bind:value={name}
					placeholder={namePlaceholder}
				/>
			</Field>

			<Field label="Descripción" for="playlist-description" optional class="min-h-0 flex-1">
				<Textarea
					id="playlist-description"
					name="description"
					maxlength={300}
					bind:value={description}
					placeholder="¿De qué va esta lista?"
					class="flex-1"
				/>
			</Field>
		</div>
	</div>

	{#if formMessage}
		<Alert tone="danger">{formMessage}</Alert>
	{/if}

	<div class="flex items-center justify-end gap-3">
		<Button type="button" variant="secondary" onclick={onCancel}>Cancelar</Button>
		<Button type="submit" disabled={submitting || name.trim() === ''}>
			{submitting ? submittingLabel : submitLabel}
		</Button>
	</div>
</form>
