<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import Button from './Button.svelte';
	import Field from './Field.svelte';
	import Input from './Input.svelte';
	import Textarea from './Textarea.svelte';
	import Alert from './Alert.svelte';
	import { ALBUM_EARLIEST_YEAR } from '$lib/config';

	interface Props {
		action: string;
		initialTitle?: string;
		initialDescription?: string;
		initialReleaseYear?: number;
		titlePlaceholder?: string;
		formMessage?: string;
		submitLabel: string;
		submittingLabel: string;
		onCancel: () => void;
		onSuccess?: () => void;
	}

	let {
		action,
		initialTitle = '',
		initialDescription = '',
		initialReleaseYear,
		titlePlaceholder = '',
		formMessage,
		submitLabel,
		submittingLabel,
		onCancel,
		onSuccess
	}: Props = $props();

	let title = $state(untrack(() => initialTitle));
	let description = $state(untrack(() => initialDescription));
	let releaseYear = $state(untrack(() => (initialReleaseYear ? String(initialReleaseYear) : '')));
	let submitting = $state(false);

	const latestYear = new Date().getFullYear() + 1;
</script>

<form
	method="POST"
	{action}
	use:enhance={() => {
		submitting = true;
		return async ({ update, result }) => {
			await update({ reset: false });
			submitting = false;
			if (result.type === 'success') onSuccess?.();
		};
	}}
	class="mt-4 w-full max-w-2xl space-y-4"
>
	<Field label="Título" for="album-title">
		<Input
			id="album-title"
			name="title"
			maxlength={200}
			required
			bind:value={title}
			placeholder={titlePlaceholder}
		/>
	</Field>

	<Field label="Año de publicación" for="album-year" optional>
		<Input
			id="album-year"
			name="releaseYear"
			type="number"
			min={ALBUM_EARLIEST_YEAR}
			max={latestYear}
			bind:value={releaseYear}
			placeholder={String(new Date().getFullYear())}
		/>
	</Field>

	<Field label="Descripción" for="album-description" optional>
		<Textarea
			id="album-description"
			name="description"
			maxlength={256}
			bind:value={description}
			placeholder="¿De qué va este álbum?"
		/>
	</Field>

	{#if formMessage}
		<Alert tone="danger">{formMessage}</Alert>
	{/if}

	<div class="flex items-center justify-end gap-3">
		<Button type="button" variant="secondary" onclick={onCancel}>Cancelar</Button>
		<Button type="submit" disabled={submitting || title.trim() === ''}>
			{submitting ? submittingLabel : submitLabel}
		</Button>
	</div>
</form>
