<script lang="ts">
	interface Props {
		playing?: boolean;
		size?: 'sm' | 'md' | 'lg';
		variant?: 'strong' | 'glass';
		label: string;
		onclick?: (event: MouseEvent) => void;
		as?: 'button' | 'span';
		class?: string;
	}

	let {
		playing = false,
		size = 'md',
		variant = 'strong',
		label,
		onclick,
		as = 'button',
		class: klass = ''
	}: Props = $props();

	const SIZE: Record<'sm' | 'md' | 'lg', string> = {
		sm: 'size-9.5',
		md: 'size-10',
		lg: 'size-11.5'
	};

	const VARIANT: Record<'strong' | 'glass', string> = {
		strong: 'bg-cta-strong text-ink hover:brightness-95',
		glass: 'border border-on-art/12 bg-ink/76 text-fg backdrop-blur'
	};

	const classes = $derived(
		`grid shrink-0 place-items-center rounded-full transition duration-150 ${SIZE[size]} ${VARIANT[variant]} ${klass}`
	);
</script>

{#snippet icon()}
	{#if playing}
		<svg width="15" height="15" viewBox="0 0 15 15" fill="currentColor">
			<rect x="3" y="1" width="3" height="13" rx="1" />
			<rect x="9" y="1" width="3" height="13" rx="1" />
		</svg>
	{:else}
		<svg width="15" height="15" viewBox="0 0 15 15" fill="currentColor">
			<path d="M3 1.5v12l10-6z" />
		</svg>
	{/if}
{/snippet}

{#if as === 'span'}
	<span aria-hidden="true" class={classes}>
		{@render icon()}
	</span>
{:else}
	<button type="button" {onclick} aria-label={label} class={classes}>
		{@render icon()}
	</button>
{/if}
