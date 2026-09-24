<script lang="ts">
	import type { Snippet } from 'svelte';
	import { enhance } from '$app/forms';
	import type { LucideIcon } from '@lucide/svelte';
	import ImageDropzone from './ImageDropzone.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';

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
		layout?: 'inline' | 'split';
		coverHint?: string;
		onCancel: () => void;
		onSuccess?: () => void;
		fields: Snippet;
		extra?: Snippet;
		aside?: Snippet;
		footer?: Snippet;
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
		layout = 'inline',
		coverHint,
		onCancel,
		onSuccess,
		fields,
		extra,
		aside,
		footer
	}: Props = $props();

	let submitting = $state(false);

	const split = $derived(layout === 'split');
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
	class={split ? 'mt-6 grid gap-8 lg:grid-cols-[auto_1fr]' : 'mt-5 w-full max-w-4xl space-y-5'}
>
	{#if split}
		<div class="flex w-full flex-col gap-4 lg:w-64">
			<ImageDropzone
				name="cover"
				fallbackUrl={coverFallbackUrl}
				required={requireCover}
				icon={coverIcon}
				gradient
				size="fill"
			/>
			{#if coverHint}
				<p class="text-xs text-fg-2">{coverHint}</p>
			{/if}
			{@render fields()}
			{@render extra?.()}
		</div>
		<div class="min-w-0">
			{@render aside?.()}
			<div class="mt-6 space-y-5">
				{@render feedback()}
				{@render actions()}
			</div>
		</div>
	{:else}
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
		{@render feedback()}
		{@render actions()}
	{/if}
</form>

{#snippet feedback()}
	{#if formMessage}
		<Alert tone="danger">{formMessage}</Alert>
	{/if}
{/snippet}

{#snippet actions()}
	<div class="flex items-center justify-between gap-4">
		{#if helperText}
			<p class="text-sm text-fg-2">{helperText}</p>
		{/if}
		{@render footer?.()}
		<div class="ml-auto flex shrink-0 items-center gap-3">
			<Button type="button" variant="secondary" onclick={onCancel}>Cancelar</Button>
			<Button type="submit" disabled={submitting || !canSubmit}>
				{submitting ? submittingLabel : submitLabel}
			</Button>
		</div>
	</div>
{/snippet}
