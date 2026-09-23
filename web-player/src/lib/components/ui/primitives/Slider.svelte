<script lang="ts">
	interface Props {
		value: number;
		max: number;
		label: string;
		oninput: (value: number) => void;
		variant: 'seek' | 'volume';
	}

	let { value, max, label, oninput, variant }: Props = $props();

	const percent = $derived(max > 0 ? (value / max) * 100 : 0);

	function handleInput(event: Event) {
		oninput(Number((event.currentTarget as HTMLInputElement).value));
	}
</script>

<input
	type="range"
	min="0"
	{max}
	{value}
	oninput={handleInput}
	aria-label={label}
	class="mf-slider mf-slider-{variant}"
	style="--p:{percent}%"
/>

<style>
	.mf-slider {
		-webkit-appearance: none;
		appearance: none;
		height: var(--mf-range-h);
		border-radius: 100px;
		outline: none;
		cursor: pointer;
		background-image:
			linear-gradient(
				to right,
				transparent var(--p),
				color-mix(in srgb, var(--mf-text) 12%, var(--mf-bg)) var(--p)
			),
			var(--mf-slider-fill);
		background-size:
			100% 100%,
			300% 100%;
		animation: gradient-drift 10s linear infinite alternate;
	}
	.mf-slider-seek {
		width: 100%;
	}
	.mf-slider-volume {
		width: var(--mf-volume-w);
	}
	.mf-slider::-webkit-slider-thumb {
		-webkit-appearance: none;
		appearance: none;
		width: var(--mf-range-thumb);
		height: var(--mf-range-thumb);
		border-radius: 50%;
		background: var(--mf-text);
		cursor: pointer;
	}
	.mf-slider::-moz-range-thumb {
		width: var(--mf-range-thumb);
		height: var(--mf-range-thumb);
		border-radius: 50%;
		background: var(--mf-text);
		border: none;
		cursor: pointer;
	}
</style>
