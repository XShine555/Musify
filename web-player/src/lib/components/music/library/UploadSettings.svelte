<script lang="ts" module>
	import type { Visibility } from '$lib/data/uploadQueue';

	const VISIBILITY: { value: Visibility; label: string }[] = [
		{ value: 'private', label: 'Privado' },
		{ value: 'public', label: 'Público' }
	];
</script>

<script lang="ts">
	import ImageIcon from '@lucide/svelte/icons/image';
	import Plus from '@lucide/svelte/icons/plus';
	import X from '@lucide/svelte/icons/x';
	import type { GenreOption } from '$lib/types';
	import { LIMITS } from '$lib/validation';
	import GenrePicker from '$lib/components/music/GenrePicker.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';
	import SegmentedControl from '$lib/components/ui/primitives/SegmentedControl.svelte';
	import Input from '$lib/components/ui/primitives/Input.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import ImageDropzone from '$lib/components/ui/forms/ImageDropzone.svelte';

	interface Props {
		genres: GenreOption[];
		selectedGenres: string[];
		visibility: Visibility;
		coauthors: string[];
		title: string;
		titleDisabled: boolean;
		class?: string;
	}

	let {
		genres,
		selectedGenres = $bindable(),
		visibility = $bindable(),
		coauthors = $bindable(),
		title = $bindable(),
		titleDisabled,
		class: klass = ''
	}: Props = $props();

	let artist = $state('');
	let hasCover = $state(false);

	function addArtist() {
		const name = artist.trim();
		if (name === '') return;
		const known = coauthors.some((current) => current.toLowerCase() === name.toLowerCase());
		if (!known) coauthors = [...coauthors, name];
		artist = '';
	}

	function onArtistKey(event: KeyboardEvent) {
		if (event.key !== 'Enter') return;
		event.preventDefault();
		addArtist();
	}
</script>

<aside class="flex flex-col gap-6 {klass}">
	<p class="text-xs font-medium tracking-eyebrow text-fg-2 uppercase">Aplicar a esta subida</p>

	<div class="grid gap-8 sm:grid-cols-[auto_1fr]">
		<Field label="Portada">
			<div class="w-full max-w-44">
				<ImageDropzone
					name="cover"
					icon={ImageIcon}
					gradient
					size="fill"
					onselect={() => (hasCover = true)}
				/>
			</div>
			{#snippet hint()}
				<p>{hasCover ? 'Portada lista' : 'Haz clic para elegir una imagen'}</p>
			{/snippet}
		</Field>

		<div class="flex min-w-0 flex-col gap-7">
			<GenrePicker {genres} bind:selected={selectedGenres} />

			<Field label="Título" for="upload-title">
				<Input
					id="upload-title"
					bind:value={title}
					maxlength={LIMITS.trackTitle}
					disabled={titleDisabled}
					placeholder={titleDisabled
						? 'Varios archivos · edita cada título en la cola'
						: 'Nombre de la canción'}
				/>
			</Field>

			<div class="grid gap-7 md:grid-cols-2">
				<Field label="Visibilidad">
					<SegmentedControl options={VISIBILITY} bind:value={visibility} />
				</Field>

				<Field label="Artistas colaboradores" for="upload-coauthor">
					<div class="flex items-center gap-2">
						<Input
							id="upload-coauthor"
							bind:value={artist}
							maxlength={LIMITS.trackTitle}
							placeholder="Nombre del artista"
							onkeydown={onArtistKey}
						/>
						<IconButton label="Añadir artista" surface onclick={addArtist}>
							<Plus class="size-icon-sm" />
						</IconButton>
					</div>
					{#if coauthors.length > 0}
						<ul class="mt-3 flex flex-wrap gap-2">
							{#each coauthors as name (name)}
								<li
									class="flex items-center gap-1 rounded-full bg-btn py-1 pr-1 pl-3.5 text-xs text-fg"
								>
									{name}
									<IconButton
										label="Quitar a {name}"
										size="xs"
										plain
										onclick={() => (coauthors = coauthors.filter((current) => current !== name))}
									>
										<X class="size-icon-xs" />
									</IconButton>
								</li>
							{/each}
						</ul>
					{/if}
					{#snippet hint()}
						<p>Aparecerán como coautores de cada canción.</p>
					{/snippet}
				</Field>
			</div>
		</div>
	</div>
</aside>
