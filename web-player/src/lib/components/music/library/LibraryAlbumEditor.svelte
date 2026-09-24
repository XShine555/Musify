<script lang="ts" module>
	const ALBUM_TYPES = [
		{ value: 'album', label: 'Álbum' },
		{ value: 'ep', label: 'EP' },
		{ value: 'single', label: 'Single' }
	] as const;

	type AlbumType = (typeof ALBUM_TYPES)[number]['value'];
</script>

<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import { page } from '$app/state';
	import Disc3 from '@lucide/svelte/icons/disc-3';
	import Plus from '@lucide/svelte/icons/plus';
	import X from '@lucide/svelte/icons/x';
	import type { Album, LibraryTrack } from '$lib/types';
	import { albumCover } from '$lib/utils/hrefs';
	import { plural } from '$lib/utils/format';
	import { ALBUM_EARLIEST_YEAR, LIMITS } from '$lib/validation';
	import ImageDropzone from '$lib/components/ui/forms/ImageDropzone.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';
	import Input from '$lib/components/ui/primitives/Input.svelte';
	import SegmentedControl from '$lib/components/ui/primitives/SegmentedControl.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import TrackPickList from './TrackPickList.svelte';

	interface Props {
		album: Album | null;
		tracks: LibraryTrack[];
		memberIds: string[];
		formMessage?: string;
		onClose: () => void;
	}

	let { album, tracks, memberIds, formMessage, onClose }: Props = $props();

	const isNew = $derived(album === null);
	const latestYear = new Date().getFullYear() + 1;

	let title = $state(untrack(() => album?.title ?? ''));
	let releaseYear = $state(untrack(() => String(album?.releaseYear ?? new Date().getFullYear())));
	let type = $state<AlbumType>('album');
	let currentIds = $state<string[]>(untrack(() => [...memberIds]));
	let submitting = $state(false);
	let confirmingDelete = $state(false);

	const initialIds = untrack(() => new Set(memberIds));
	const current = $derived(new Set(currentIds));
	const inAlbum = $derived(tracks.filter((track) => current.has(track.id)));
	const available = $derived(tracks.filter((track) => !current.has(track.id)));
	const addedIds = $derived(currentIds.filter((id) => !initialIds.has(id)));
	const removedIds = $derived([...initialIds].filter((id) => !current.has(id)));

	function add(track: LibraryTrack) {
		currentIds = [...currentIds, track.id];
	}

	function remove(track: LibraryTrack) {
		currentIds = currentIds.filter((id) => id !== track.id);
	}
</script>

<div>
	<form
		method="POST"
		action="?/saveAlbum"
		enctype="multipart/form-data"
		use:enhance={() => {
			submitting = true;
			return async ({ update, result }) => {
				await update({ reset: false });
				submitting = false;
				if (result.type === 'success') onClose();
			};
		}}
		class="mt-6 grid gap-8 lg:grid-cols-[auto_1fr]"
	>
		<input type="hidden" name="albumId" value={album?.id ?? ''} />
		<input type="hidden" name="addTrackIds" value={addedIds.join(',')} />
		<input type="hidden" name="removeTrackIds" value={removedIds.join(',')} />

		<div class="flex w-full flex-col gap-4 lg:w-64">
			<ImageDropzone
				name="cover"
				fallbackUrl={album ? albumCover(album.id, 'large') : undefined}
				required={isNew}
				icon={Disc3}
				gradient
				size="fill"
			/>
			<p class="text-xs text-fg-2">Las canciones del álbum usan esta portada.</p>

			<Field label="Título" for="album-title">
				<Input
					id="album-title"
					name="title"
					maxlength={LIMITS.albumTitle}
					required
					bind:value={title}
					placeholder="Sin título"
				/>
			</Field>

			<Field label="Artista principal" for="album-artist">
				<Input id="album-artist" value={page.data.user?.name ?? ''} disabled />
			</Field>

			<Field label="Tipo">
				<SegmentedControl options={[...ALBUM_TYPES]} bind:value={type} />
			</Field>

			<Field label="Año de lanzamiento" for="album-year">
				<Input
					id="album-year"
					name="releaseYear"
					type="number"
					min={ALBUM_EARLIEST_YEAR}
					max={latestYear}
					bind:value={releaseYear}
				/>
			</Field>
		</div>

		<div class="min-w-0">
			<div class="flex items-start justify-between gap-4">
				<div>
					<h2 class="text-display-3">Canciones</h2>
					<p class="mt-1 text-sm text-fg-2">
						{inAlbum.length > 0 ? plural(inAlbum.length, 'canción', 'canciones') : 'Sin canciones'}
					</p>
				</div>
				<Button variant="secondary" size="sm" onclick={onClose}>Cerrar</Button>
			</div>

			{#if available.length > 0}
				<h3 class="mt-6 mb-2 px-3 text-eyebrow text-fg-2">Añadir desde tu biblioteca</h3>
				<TrackPickList tracks={available} icon={Plus} onPick={add} />
			{/if}

			<div class="mt-6">
				{#if inAlbum.length > 0}
					<h3 class="mb-2 px-3 text-eyebrow text-fg-2">En este álbum</h3>
					<TrackPickList tracks={inAlbum} icon={X} onPick={remove} />
				{:else}
					<EmptyState
						title="Este álbum aún no tiene canciones"
						description="Añádelas desde tu biblioteca o sube archivos nuevos."
					/>
				{/if}
			</div>

			{#if formMessage}
				<Alert tone="danger" class="mt-6">{formMessage}</Alert>
			{/if}

			<div class="mt-8 flex items-center justify-between gap-4">
				<Button type="submit" disabled={submitting || title.trim() === ''}>
					{#if submitting}
						{isNew ? 'Creando…' : 'Guardando…'}
					{:else}
						{isNew ? 'Crear álbum' : 'Guardar cambios'}
					{/if}
				</Button>
				{#if !isNew}
					<button
						type="button"
						onclick={() => (confirmingDelete = true)}
						class="text-sm text-danger-fg transition-opacity hover:opacity-80"
					>
						Eliminar álbum
					</button>
				{/if}
			</div>
		</div>
	</form>
</div>

{#if album}
	<ConfirmDialog
		open={confirmingDelete}
		onClose={() => (confirmingDelete = false)}
		title="¿Eliminar «{album.title}»?"
		description="Las canciones no se borran: solo se elimina el álbum."
		confirmLabel="Eliminar"
		action="?/deleteAlbum"
		fields={{ albumId: album.id }}
	/>
{/if}
