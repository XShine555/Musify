<script lang="ts">
	import ImageIcon from '@lucide/svelte/icons/image';
	import type { LucideIcon } from '@lucide/svelte';

	interface Props {
		name: string;
		fallbackUrl?: string;
		required?: boolean;
		icon?: LucideIcon;
		gradient?: boolean;
		size?: 'default' | 'hero';
		onselect?: (file: File) => void;
		class?: string;
	}

	let {
		name,
		fallbackUrl,
		required = false,
		icon: Icon = ImageIcon,
		gradient = false,
		size = 'default',
		onselect,
		class: klass = ''
	}: Props = $props();

	const SIZE: Record<'default' | 'hero', string> = {
		default: 'size-40 sm:size-52 rounded-control',
		hero: 'size-cover-hero rounded-art-lg'
	};

	let preview = $state('');
	let fallbackFailed = $state(false);

	const hasImage = $derived(preview !== '' || (!!fallbackUrl && !fallbackFailed));

	function onInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (!file) return;
		preview = URL.createObjectURL(file);
		onselect?.(file);
	}
</script>

<label
	class="relative grid cursor-pointer place-items-center overflow-hidden transition-colors {gradient
		? ''
		: 'border border-line bg-surface hover:border-accent/50'} {SIZE[size]} {klass}"
	style={gradient && !hasImage ? 'background:var(--mf-cover-grad)' : undefined}
>
	<input
		type="file"
		{name}
		accept="image/*"
		{required}
		onchange={onInput}
		class="absolute inset-0 cursor-pointer opacity-0"
		aria-label="Seleccionar portada"
	/>
	{#if preview !== ''}
		<img src={preview} alt="Portada" class="h-full w-full object-cover" />
	{:else if fallbackUrl && !fallbackFailed}
		<img
			src={fallbackUrl}
			alt="Portada actual"
			loading="lazy"
			onerror={() => (fallbackFailed = true)}
			class="h-full w-full object-cover"
		/>
	{:else}
		<Icon class="size-icon-xl {gradient ? 'text-on-art/70' : 'text-fg-2'}" />
	{/if}
</label>
