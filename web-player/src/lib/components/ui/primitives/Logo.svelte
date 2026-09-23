<script lang="ts">
	interface Props {
		size?: 'sm' | 'lg';
	}

	let { size = 'sm' }: Props = $props();

	const TEXT: Record<'sm' | 'lg', string> = {
		sm: 'font-display text-lg font-semibold tracking-tight text-fg',
		lg: 'font-display text-[clamp(1.5rem,3vw,2.5rem)] font-semibold tracking-tight'
	};
</script>

<span class={TEXT[size]}>Musify</span>
