<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import Disc3 from '@lucide/svelte/icons/disc-3';
	import ImageDropzone from './ImageDropzone.svelte';
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
		coverFallbackUrl?: string;
		requireCover?: boolean;
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
		coverFallbackUrl,
		requireCover = false,
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
	enctype="multipart/form-data"
	use:enhance={() => {
		submitting = true;
		return async ({ update, result }) => {
			await update({ reset: false });
			submitting = false;
			if (result.type === 'success') onSuccess?.();
		};
	}}
	class="mt-5 w-full max-w-2xl space-y-5"
>
	<div class="flex flex-col items-stretch gap-5 sm:flex-row sm:gap-8">
		<Field>
			<ImageDropzone
				name="cover"
				fallbackUrl={coverFallbackUrl}
				required={requireCover}
				icon={Disc3}
				gradient
				class="h-36 w-36 rounded-2xl"
			/>
		</Field>

		<div class="flex flex-1 flex-col gap-4">
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

			<Field label="Año de publicación" for="album-year">
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
		</div>
	</div>

	<Field label="Descripción" for="album-description">
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
