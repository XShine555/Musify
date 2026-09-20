<script lang="ts">
	import { untrack } from 'svelte';
	import Music from '@lucide/svelte/icons/music';
	import CoverForm from './CoverForm.svelte';
	import Field from './Field.svelte';
	import Input from './Input.svelte';
	import Textarea from './Textarea.svelte';
	import SegmentedControl from './SegmentedControl.svelte';

	const NAME_MAX_LENGTH = 60;
	const DESCRIPTION_MAX_LENGTH = 300;

	interface Props {
		action: string;
		initialName?: string;
		initialDescription?: string;
		initialVisibility?: 'private' | 'public';
		namePlaceholder?: string;
		coverFallbackUrl?: string;
		formMessage?: string;
		submitLabel: string;
		submittingLabel: string;
		helperText?: string;
		onCancel: () => void;
		onSuccess?: () => void;
	}

	let {
		action,
		initialName = '',
		initialDescription = '',
		initialVisibility = 'private',
		namePlaceholder = 'Playlist sin título',
		coverFallbackUrl,
		formMessage,
		submitLabel,
		submittingLabel,
		helperText,
		onCancel,
		onSuccess
	}: Props = $props();

	let name = $state(untrack(() => initialName));
	let description = $state(untrack(() => initialDescription));
	let visibility = $state(untrack(() => initialVisibility));
</script>

<CoverForm
	{action}
	{coverFallbackUrl}
	coverIcon={Music}
	{formMessage}
	{submitLabel}
	{submittingLabel}
	canSubmit={name.trim() !== ''}
	{helperText}
	{onCancel}
	{onSuccess}
>
	{#snippet fields()}
		<input type="hidden" name="visibility" value={visibility} />
		<Field label="Nombre" for="playlist-name">
			{#snippet hint()}
				<span class="ml-auto tabular-nums">{name.length}/{NAME_MAX_LENGTH}</span>
			{/snippet}
			<Input
				id="playlist-name"
				name="name"
				maxlength={NAME_MAX_LENGTH}
				required
				bind:value={name}
				placeholder={namePlaceholder}
			/>
		</Field>

		<Field label="Descripción" for="playlist-description" class="min-h-0 flex-1">
			<Textarea
				id="playlist-description"
				name="description"
				maxlength={DESCRIPTION_MAX_LENGTH}
				bind:value={description}
				placeholder="Para qué sirve esta playlist, cuándo la escuchas…"
				class="flex-1"
			/>
		</Field>
	{/snippet}
	{#snippet extra()}
		<div class="flex items-center justify-between gap-4 rounded-art bg-surface px-4 py-3.5">
			<div class="min-w-0">
				<div class="text-sm font-medium text-fg-2">Visibilidad</div>
				<div class="mt-0.5 text-xs text-muted">
					{visibility === 'private'
						? 'Solo tú la ves en tu biblioteca'
						: 'Cualquiera con el enlace puede verla'}
				</div>
			</div>
			<SegmentedControl
				bind:value={visibility}
				options={[
					{ value: 'private', label: 'Privada' },
					{ value: 'public', label: 'Pública' }
				]}
			/>
		</div>
	{/snippet}
</CoverForm>
