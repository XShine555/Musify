<script lang="ts">
	interface Props {
		name: string;
		imageUrl?: string | null;
		size?: number;
	}

	let { name, imageUrl, size = 108 }: Props = $props();

	const initials = $derived(
		name
			.split(/\s+/)
			.filter(Boolean)
			.slice(0, 2)
			.map((part) => part[0]?.toUpperCase() ?? '')
			.join('')
	);
</script>

<div class="flex flex-col items-center gap-2.5 text-center" style="width:{size}px">
	<div
		class="relative grid shrink-0 place-items-center overflow-hidden rounded-full bg-surface shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/artist:shadow-art-lg"
		style="width:{size}px;height:{size}px"
	>
		{#if imageUrl}
			<img src={imageUrl} alt="" class="h-full w-full object-cover" />
		{:else}
			<div
				class="pointer-events-none absolute inset-0"
				style="background:repeating-linear-gradient(135deg, color-mix(in oklch, var(--mf-text) 12%, transparent) 0 2px, transparent 2px 10px)"
			></div>
			<span class="relative text-lg font-semibold text-fg">{initials}</span>
		{/if}
	</div>
	<div class="w-full truncate text-sm text-fg">{name}</div>
</div>
