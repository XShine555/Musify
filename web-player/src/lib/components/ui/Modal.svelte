<script lang="ts">
	import type { Snippet } from 'svelte';
	import X from '@lucide/svelte/icons/x';
	import IconButton from './IconButton.svelte';

	interface Props {
		open: boolean;
		onClose: () => void;
		title?: string;
		maxWidth?: string;
		panelClass?: string;
		children: Snippet;
	}

	let {
		open,
		onClose,
		title,
		maxWidth = 'max-w-2xl',
		panelClass = 'border border-line bg-elevated',
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
		class="animate-fade fixed inset-0 z-[60] grid place-items-center bg-black/70 p-4 backdrop-blur-sm"
		role="presentation"
		onclick={onBackdropClick}
	>
		<div
			bind:this={panel}
			class="animate-pop w-full {maxWidth} rounded-panel p-6 shadow-2xl outline-none {panelClass}"
			role="dialog"
			aria-modal="true"
			aria-label={title}
			tabindex="-1"
		>
			{#if title}
				<div class="flex items-center justify-between">
					<h2 class="text-lg font-bold tracking-tight text-fg">{title}</h2>
					<IconButton label="Cerrar" onclick={onClose}>
						<X class="h-4 w-4" />
					</IconButton>
				</div>
			{/if}
			{@render children()}
		</div>
	</div>
{/if}
