<script lang="ts">
	import { untrack } from 'svelte';
	import Disc3 from '@lucide/svelte/icons/disc-3';
	import CoverForm from './CoverForm.svelte';
	import Field from './Field.svelte';
	import Input from './Input.svelte';
	import Textarea from './Textarea.svelte';
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

	const latestYear = new Date().getFullYear() + 1;
</script>

<CoverForm
	{action}
	{coverFallbackUrl}
	{requireCover}
	coverIcon={Disc3}
	{formMessage}
	{submitLabel}
	{submittingLabel}
	canSubmit={title.trim() !== ''}
	{onCancel}
	{onSuccess}
>
	{#snippet fields()}
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
	{/snippet}
	{#snippet extra()}
		<Field label="Descripción" for="album-description">
			<Textarea
				id="album-description"
				name="description"
				maxlength={256}
				bind:value={description}
				placeholder="¿De qué va este álbum?"
			/>
		</Field>
	{/snippet}
</CoverForm>
