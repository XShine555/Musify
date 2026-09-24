<script lang="ts">
	import type { GenreOption } from '$lib/types';
	import { genreOptions } from '$lib/data/genres';
	import Chip from '$lib/components/ui/primitives/Chip.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';

	interface Props {
		genres: GenreOption[];
		selected: string[];
		name?: string;
		help?: string;
	}

	let {
		genres,
		selected = $bindable(),
		name,
		help = 'Elige varios. Algunos no se pueden combinar.'
	}: Props = $props();

	const options = $derived(genreOptions(genres));
	const blocked = $derived(
		new Set(
			options
				.filter((option) => selected.includes(option.genre))
				.flatMap((option) => option.incompatibleWith)
		)
	);

	function toggle(genre: string) {
		selected = selected.includes(genre)
			? selected.filter((current) => current !== genre)
			: [...selected, genre];
	}
</script>

<Field label="Géneros">
	<div class="flex flex-wrap gap-2">
		{#each options as option (option.genre)}
			<Chip
				selected={selected.includes(option.genre)}
				disabled={blocked.has(option.genre)}
				onclick={() => toggle(option.genre)}
			>
				{option.label}
			</Chip>
		{/each}
	</div>
	{#if name}
		{#each selected as genre (genre)}
			<input type="hidden" {name} value={genre} />
		{/each}
	{/if}
	{#snippet hint()}
		<p>{help}</p>
		<span class="shrink-0 tabular-nums">{selected.length} elegidos</span>
	{/snippet}
</Field>
