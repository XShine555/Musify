<script lang="ts">
	import type { Snippet } from 'svelte';
	import X from '@lucide/svelte/icons/x';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import { fade } from 'svelte/transition';
	import { pop } from '$lib/utils/transitions';
	import { onEscape } from '$lib/actions/onEscape';

	interface Props {
		open: boolean;
		onClose: () => void;
		eyebrow?: string;
		title?: string;
		maxWidth?: string;
		panelClass?: string;
		children: Snippet;
	}

	let {
		open,
		onClose,
		eyebrow,
		title,
		maxWidth = 'max-w-2xl',
		panelClass = '',
		children
	}: Props = $props();

	let panel: HTMLDivElement | undefined = $state();

	$effect(() => {
		if (!open) return;
		const trigger = document.activeElement as HTMLElement | null;
		panel?.focus();
		return () => trigger?.focus();
	});

	function onBackdropClick(event: MouseEvent) {
		if (event.target === event.currentTarget) onClose();
	}

	function focusableElements(): HTMLElement[] {
		if (!panel) return [];
		return Array.from(
			panel.querySelectorAll<HTMLElement>(
				'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
			)
		);
	}

	function onWindowKeydown(event: KeyboardEvent) {
		if (!open || event.key !== 'Tab') return;
		const elements = focusableElements();
		if (elements.length === 0) return;
		const first = elements[0];
		const last = elements[elements.length - 1];
		if (event.shiftKey && document.activeElement === first) {
			event.preventDefault();
			last.focus();
		} else if (!event.shiftKey && document.activeElement === last) {
			event.preventDefault();
			first.focus();
		}
	}
</script>

<svelte:window onkeydown={onWindowKeydown} />

{#if open}
	<div
		transition:fade={{ duration: 200 }}
		class="fixed inset-0 z-(--z-modal) grid place-items-center bg-scrim/70 p-4 backdrop-blur-sm"
		role="presentation"
		onclick={onBackdropClick}
	>
		<div
			bind:this={panel}
			use:onEscape={onClose}
			transition:pop={{ duration: 200 }}
			class="glass-panel max-h-[85dvh] w-full glow-drift overflow-y-auto {maxWidth} rounded-panel p-5 outline-none sm:p-6 {panelClass}"
			role="dialog"
			aria-modal="true"
			aria-label={title}
			tabindex="-1"
		>
			{#if title}
				<div class="flex items-center justify-between gap-4">
					<div class="min-w-0">
						{#if eyebrow}
							<p class="mb-2.5 text-eyebrow text-fg-2">
								{eyebrow}
							</p>
						{/if}
						<h2 class="text-display-3 text-fg">
							{title}
						</h2>
					</div>
					<IconButton label="Cerrar" onclick={onClose} class="shrink-0">
						<X class="size-icon-sm" />
					</IconButton>
				</div>
			{/if}
			{@render children()}
		</div>
	</div>
{/if}
