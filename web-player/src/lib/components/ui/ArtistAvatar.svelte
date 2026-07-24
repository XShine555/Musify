<script lang="ts">
	interface Props {
		name: string;
		size?: number;
	}

	let { name, size = 108 }: Props = $props();

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
		<div
			class="pointer-events-none absolute inset-0"
			style="background:repeating-linear-gradient(135deg, rgba(255,255,255,0.12) 0 2px, transparent 2px 10px)"
		></div>
		<span class="relative text-lg font-semibold text-white/90">{initials}</span>
	</div>
	<div class="w-full truncate text-sm text-fg">{name}</div>
</div>
