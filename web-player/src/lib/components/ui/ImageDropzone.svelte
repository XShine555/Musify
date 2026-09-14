<script lang="ts">
	import ImageIcon from '@lucide/svelte/icons/image';
	import type { LucideIcon } from '@lucide/svelte';

	interface Props {
		name: string;
		fallbackUrl?: string;
		required?: boolean;
		icon?: LucideIcon;
		gradient?: boolean;
		class?: string;
	}

	let {
		name,
		fallbackUrl,
		required = false,
		icon: Icon = ImageIcon,
		gradient = false,
		class: klass = 'h-40 w-40 sm:h-52 sm:w-52 rounded-control'
	}: Props = $props();

	let preview = $state('');
	let fallbackFailed = $state(false);

	const hasImage = $derived(preview !== '' || (!!fallbackUrl && !fallbackFailed));

	function onInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (file) preview = URL.createObjectURL(file);
	}
</script>

<label
	class="relative grid cursor-pointer place-items-center overflow-hidden transition-colors {gradient
		? ''
		: 'border border-line bg-surface hover:border-accent/50'} {klass}"
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
		<Icon class="h-9 w-9 {gradient ? 'text-white/70' : 'text-muted'}" strokeWidth={1.3} />
	{/if}
</label>
