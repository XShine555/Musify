<script lang="ts">
	import ImageIcon from '@lucide/svelte/icons/image';
	import type { GenreOption } from '$lib/types';
	import GenrePicker from '$lib/components/music/GenrePicker.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';
	import ImageDropzone from '$lib/components/ui/forms/ImageDropzone.svelte';

	interface AlbumChoice {
		id: string;
		title: string;
	}

	interface Props {
		genres: GenreOption[];
		albums: AlbumChoice[];
		selectedGenres: string[];
		albumId: string;
		hasCover: boolean;
		oncover: (file: File) => void;
		disabled?: boolean;
		class?: string;
	}

	let {
		genres,
		albums,
		selectedGenres = $bindable(),
		albumId = $bindable(),
		hasCover,
		oncover,
		disabled = false,
		class: klass = ''
	}: Props = $props();
</script>

<aside class="flex flex-col gap-6 {klass}">
	<p class="text-eyebrow text-fg-2">Aplicar a toda la cola</p>

	<div class="grid gap-8 sm:grid-cols-[auto_1fr]">
		<Field label="Portada">
			<div class="w-full max-w-cover-fill">
				<ImageDropzone name="cover" icon={ImageIcon} gradient size="fill" onselect={oncover} />
			</div>
			{#snippet hint()}
				<p>{hasCover ? 'Portada lista' : 'Opcional. Haz clic para elegir una imagen'}</p>
			{/snippet}
		</Field>

		<div class="flex min-w-0 flex-col gap-7">
			<GenrePicker {genres} bind:selected={selectedGenres} />

			<Field label="Añadir a un álbum" for="upload-album">
				<select id="upload-album" class="field-control px-4" bind:value={albumId} {disabled}>
					<option value="">Ninguno</option>
					{#each albums as album (album.id)}
						<option value={album.id}>{album.title}</option>
					{/each}
				</select>
				{#snippet hint()}
					<p>
						¿Otro álbum? <a class="text-fg underline" href="/library/albums/new">Crea uno nuevo</a>
						y vuelve.
					</p>
				{/snippet}
			</Field>
		</div>
	</div>
</aside>
