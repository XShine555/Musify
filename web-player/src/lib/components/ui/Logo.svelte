<script lang="ts">
	interface Props {
		size?: 'sm' | 'lg';
	}

	let { size = 'sm' }: Props = $props();

	const MARK: Record<'sm' | 'lg', string> = {
		sm: 'size-5.5 rounded-lg',
		lg: 'size-8 rounded-control'
	};

	const TEXT: Record<'sm' | 'lg', string> = {
		sm: 'font-display text-lg font-semibold tracking-tight text-fg',
		lg: 'font-display text-[clamp(1.5rem,3vw,2.5rem)] font-semibold tracking-tight'
	};
</script>

<span class="flex items-center gap-3">
	<span class="{MARK[size]} bg-[image:var(--mf-logo-grad)]"></span>
	<span class={TEXT[size]}>Musify</span>
</span>
