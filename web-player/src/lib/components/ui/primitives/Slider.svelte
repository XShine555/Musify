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
	style="background:linear-gradient(to right, var(--mf-text-2) {percent}%, var(--mf-track) {percent}%)"
/>

<style>
	.mf-slider {
		-webkit-appearance: none;
		appearance: none;
		height: var(--mf-range-h);
		border-radius: 100px;
		outline: none;
		cursor: pointer;
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
