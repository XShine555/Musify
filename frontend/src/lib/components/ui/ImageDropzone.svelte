<script lang="ts">
	import ImageIcon from '@lucide/svelte/icons/image';

	interface Props {
		name: string;
		fallbackUrl?: string;
		class?: string;
	}

	let {
		name,
		fallbackUrl,
		class: klass = 'h-52 w-52 rounded-control border border-line bg-surface hover:border-accent/50'
	}: Props = $props();

	let preview = $state('');
	let fallbackFailed = $state(false);

	function onInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (file) preview = URL.createObjectURL(file);
	}
</script>

<label class="relative grid cursor-pointer place-items-center overflow-hidden transition-colors {klass}">
	<input
		type="file"
		{name}
		accept="image/*"
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
		<ImageIcon class="h-12 w-12 text-muted" />
	{/if}
</label>
