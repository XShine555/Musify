<script lang="ts">
	import type { Snippet } from 'svelte';
	import { enhance } from '$app/forms';
	import type { LucideIcon } from '@lucide/svelte';
	import ImageDropzone from './ImageDropzone.svelte';
	import Button from '../primitives/Button.svelte';
	import Field from '../primitives/Field.svelte';
	import Alert from '../primitives/Alert.svelte';

	interface Props {
		action: string;
		coverFallbackUrl?: string;
		coverIcon: LucideIcon;
		requireCover?: boolean;
		formMessage?: string;
		submitLabel: string;
		submittingLabel: string;
		canSubmit: boolean;
		helperText?: string;
		onCancel: () => void;
		onSuccess?: () => void;
		fields: Snippet;
		extra?: Snippet;
	}

	let {
		action,
		coverFallbackUrl,
		coverIcon,
		requireCover = false,
		formMessage,
		submitLabel,
		submittingLabel,
		canSubmit,
		helperText,
		onCancel,
		onSuccess,
		fields,
		extra
	}: Props = $props();

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
			if (result.type === 'success' || result.type === 'redirect') onSuccess?.();
		};
	}}
	class="mt-5 w-full max-w-4xl space-y-5"
>
	<div class="flex flex-col items-stretch gap-5 sm:flex-row sm:gap-8">
		<Field>
			<ImageDropzone
				name="cover"
				fallbackUrl={coverFallbackUrl}
				required={requireCover}
				icon={coverIcon}
				gradient
				size="hero"
			/>
		</Field>
		<div class="flex flex-1 flex-col gap-4">
			{@render fields()}
		</div>
	</div>

	{@render extra?.()}

	{#if formMessage}
		<Alert tone="danger">{formMessage}</Alert>
	{/if}

	<div class="flex items-center justify-between gap-4">
		{#if helperText}
			<p class="text-sm text-fg-2">{helperText}</p>
		{/if}
		<div class="ml-auto flex shrink-0 items-center gap-3">
			<Button type="button" variant="secondary" onclick={onCancel}>Cancelar</Button>
			<Button type="submit" disabled={submitting || !canSubmit}>
				{submitting ? submittingLabel : submitLabel}
			</Button>
		</div>
	</div>
</form>
