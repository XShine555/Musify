<script lang="ts">
	type Size = 'sm' | 'md' | 'lg' | 'hero';

	interface Props {
		name: string;
		src?: string | null;
		size: Size;
		class?: string;
	}

	let { name, src, size, class: klass = '' }: Props = $props();

	const DIMENSION: Record<Size, string> = {
		sm: 'size-cover-xs',
		md: 'size-cover-lg',
		lg: 'size-cover-xl',
		hero: 'size-cover-hero'
	};

	const TEXT: Record<Size, string> = {
		sm: 'text-sm',
		md: 'text-base',
		lg: 'text-xl',
		hero: 'text-3xl'
	};

	const initials = $derived(
		name
			.split(/\s+/)
			.filter(Boolean)
			.slice(0, 2)
			.map((part) => part[0]?.toUpperCase() ?? '')
			.join('')
	);

	let failedSrc = $state<string | null>(null);
</script>

<div
	class="relative grid shrink-0 place-items-center overflow-hidden rounded-full bg-surface-2 {DIMENSION[
		size
	]} {klass}"
>
	{#if src && failedSrc !== src}
		<img
			{src}
			alt=""
			loading="lazy"
			decoding="async"
			onerror={() => (failedSrc = src ?? null)}
			class="absolute inset-0 h-full w-full object-cover"
		/>
	{:else}
		<span class="font-semibold text-fg uppercase {TEXT[size]}">{initials}</span>
	{/if}
</div>
