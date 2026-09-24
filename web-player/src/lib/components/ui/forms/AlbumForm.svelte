<script lang="ts" module>
	const ALBUM_TYPES = [
		{ value: 'album', label: 'Álbum' },
		{ value: 'ep', label: 'EP' },
		{ value: 'single', label: 'Single' }
	] as const;

	export type AlbumType = (typeof ALBUM_TYPES)[number]['value'];
</script>

<script lang="ts">
	import { untrack, type Snippet } from 'svelte';
	import Disc3 from '@lucide/svelte/icons/disc-3';
	import CoverForm from './CoverForm.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';
	import Input from '$lib/components/ui/primitives/Input.svelte';
	import SegmentedControl from '$lib/components/ui/primitives/SegmentedControl.svelte';
	import Textarea from '$lib/components/ui/primitives/Textarea.svelte';
	import { ALBUM_EARLIEST_YEAR, LIMITS } from '$lib/validation';

	interface Props {
		action: string;
		initialTitle?: string;
		initialDescription?: string;
		initialReleaseYear?: number;
		initialType?: AlbumType;
		titlePlaceholder?: string;
		coverFallbackUrl?: string;
		coverHint?: string;
		requireCover?: boolean;
		formMessage?: string;
		submitLabel: string;
		submittingLabel: string;
		layout?: 'inline' | 'split';
		artist?: string;
		showType?: boolean;
		aside?: Snippet;
		footer?: Snippet;
		onCancel: () => void;
		onSuccess?: () => void;
	}

	let {
		action,
		initialTitle = '',
		initialDescription = '',
		initialReleaseYear,
		initialType = 'album',
		titlePlaceholder = '',
		coverFallbackUrl,
		coverHint,
		requireCover = false,
		formMessage,
		submitLabel,
		submittingLabel,
		layout = 'inline',
		artist,
		showType = false,
		aside,
		footer,
		onCancel,
		onSuccess
	}: Props = $props();

	let title = $state(untrack(() => initialTitle));
	let description = $state(untrack(() => initialDescription));
	let releaseYear = $state(untrack(() => (initialReleaseYear ? String(initialReleaseYear) : '')));
	let type = $state<AlbumType>(untrack(() => initialType));

	const latestYear = new Date().getFullYear() + 1;
</script>

<CoverForm
	{action}
	{coverFallbackUrl}
	{coverHint}
	{requireCover}
	coverIcon={Disc3}
	{formMessage}
	{submitLabel}
	{submittingLabel}
	canSubmit={title.trim() !== ''}
	{layout}
	{aside}
	{footer}
	{onCancel}
	{onSuccess}
>
	{#snippet fields()}
		<Field label="Título" for="album-title">
			{#snippet hint()}
				<span class="ml-auto tabular-nums">{title.length}/{LIMITS.albumTitle}</span>
			{/snippet}
			<Input
				id="album-title"
				name="title"
				maxlength={LIMITS.albumTitle}
				required
				bind:value={title}
				placeholder={titlePlaceholder}
			/>
		</Field>

		{#if artist !== undefined}
			<Field label="Artista principal" for="album-artist">
				<Input id="album-artist" value={artist} disabled />
			</Field>
		{/if}

		{#if showType}
			<Field label="Tipo">
				<SegmentedControl options={[...ALBUM_TYPES]} bind:value={type} />
				<input type="hidden" name="type" value={type} />
			</Field>
		{/if}

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
				maxlength={LIMITS.albumDescription}
				bind:value={description}
				placeholder="¿De qué va este álbum?"
			/>
		</Field>
	{/snippet}
</CoverForm>
