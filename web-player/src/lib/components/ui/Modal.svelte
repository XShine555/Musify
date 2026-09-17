<script lang="ts">
	import type { Snippet } from 'svelte';
	import X from '@lucide/svelte/icons/x';
	import IconButton from './IconButton.svelte';

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
		if (!open) return;
		if (event.key === 'Escape') {
			onClose();
			return;
		}
		if (event.key !== 'Tab') return;
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
		class="animate-fade fixed inset-0 z-[60] grid place-items-center bg-scrim/70 p-4 backdrop-blur-sm"
		role="presentation"
		onclick={onBackdropClick}
	>
		<div
			bind:this={panel}
			class="animate-pop max-h-[85dvh] w-full overflow-y-auto {maxWidth} rounded-panel border border-hairline p-5 backdrop-blur-xl transition-[background] duration-500 outline-none sm:p-6 {panelClass}"
			style="background-color:var(--mf-panel-bg); background-image:var(--mf-modal-glow)"
			role="dialog"
			aria-modal="true"
			aria-label={title}
			tabindex="-1"
		>
			{#if title}
				<div class="flex items-center justify-between gap-4">
					<div class="min-w-0">
						{#if eyebrow}
							<p class="mb-2.5 text-xs font-semibold tracking-widest text-muted uppercase">
								{eyebrow}
							</p>
						{/if}
						<h2 class="font-display text-xl font-semibold tracking-[-0.02em] text-fg">
							{title}
						</h2>
					</div>
					<IconButton label="Cerrar" onclick={onClose} class="shrink-0">
						<X class="h-4 w-4" />
					</IconButton>
				</div>
			{/if}
			{@render children()}
		</div>
	</div>
{/if}
