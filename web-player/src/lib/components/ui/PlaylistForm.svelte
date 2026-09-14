<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import Music from '@lucide/svelte/icons/music';
	import ImageDropzone from './ImageDropzone.svelte';
	import Button from './Button.svelte';
	import Field from './Field.svelte';
	import Input from './Input.svelte';
	import Textarea from './Textarea.svelte';
	import Alert from './Alert.svelte';

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
	let submitting = $state(false);
	let visibility = $state(untrack(() => initialVisibility));
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
			if (result.type === 'success' || result.type === 'redirect') onSuccess?.();
		};
	}}
	class="mt-5 w-full max-w-4xl space-y-5"
>
	<input type="hidden" name="visibility" value={visibility} />
	<div class="flex flex-col items-stretch gap-5 sm:flex-row sm:gap-8">
		<Field>
			<ImageDropzone
				name="cover"
				fallbackUrl={coverFallbackUrl}
				icon={Music}
				gradient
				class="h-36 w-36 rounded-[16px]"
			/>
		</Field>

		<div class="flex flex-1 flex-col gap-4">
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
		</div>
	</div>

	<div class="flex items-center justify-between gap-4 rounded-[14px] bg-surface px-4 py-3.5">
		<div class="min-w-0">
			<div class="text-sm font-medium text-fg-2">Visibilidad</div>
			<div class="mt-0.5 text-xs text-muted">
				{visibility === 'private'
					? 'Solo tú la ves en tu biblioteca'
					: 'Cualquiera con el enlace puede verla'}
			</div>
		</div>
		<div class="relative flex shrink-0 rounded-full bg-surface-2 p-1">
			<div
				class="absolute inset-y-1 left-1 w-[calc(50%-4px)] rounded-full bg-fg transition-transform duration-300 ease-out {visibility ===
				'public'
					? 'translate-x-full'
					: ''}"
			></div>
			<button
				type="button"
				onclick={() => (visibility = 'private')}
				class="relative z-10 w-20 rounded-full py-1.5 text-center text-xs font-semibold transition-colors {visibility ===
				'private'
					? 'text-ink'
					: 'text-muted hover:text-fg-2'}"
			>
				Privada
			</button>
			<button
				type="button"
				onclick={() => (visibility = 'public')}
				class="relative z-10 w-20 rounded-full py-1.5 text-center text-xs font-semibold transition-colors {visibility ===
				'public'
					? 'text-ink'
					: 'text-muted hover:text-fg-2'}"
			>
				Pública
			</button>
		</div>
	</div>

	{#if formMessage}
		<Alert tone="danger">{formMessage}</Alert>
	{/if}

	<div class="flex items-center justify-between gap-4">
		{#if helperText}
			<p class="text-sm text-muted">{helperText}</p>
		{:else}
			<span></span>
		{/if}
		<div class="flex shrink-0 items-center gap-3">
			<Button type="button" variant="secondary" onclick={onCancel}>Cancelar</Button>
			<Button type="submit" disabled={submitting || name.trim() === ''}>
				{submitting ? submittingLabel : submitLabel}
			</Button>
		</div>
	</div>
</form>
