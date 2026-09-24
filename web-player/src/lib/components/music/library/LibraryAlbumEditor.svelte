<script lang="ts">
	import { beforeNavigate } from '$app/navigation';
	import type { Album, LibraryTrack } from '$lib/types';
	import { albumCover } from '$lib/utils/hrefs';
	import AlbumForm from '$lib/components/ui/forms/AlbumForm.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import AlbumTrackPicker from './AlbumTrackPicker.svelte';

	interface Props {
		album: Album | null;
		artist: string;
		tracks: LibraryTrack[];
		memberIds: string[];
		formMessage?: string;
		onClose: () => void;
	}

	let { album, artist, tracks, memberIds, formMessage, onClose }: Props = $props();

	const isNew = $derived(album === null);

	let confirmingDelete = $state(false);
	let dirty = $state(false);

	beforeNavigate((navigation) => {
		if (!dirty || confirmingDelete) return;
		if (!confirm('Tienes cambios sin guardar. ¿Salir sin guardar?')) navigation.cancel();
	});

	function saved() {
		dirty = false;
		onClose();
	}
</script>

<div oninput={() => (dirty = true)} role="presentation">
	<AlbumForm
		action="?/saveAlbum"
		layout="split"
		{artist}
		showType
		initialType={album ? (album.type.toLowerCase() as 'album' | 'ep' | 'single') : 'album'}
		initialTitle={album?.title}
		initialDescription={album?.description ?? ''}
		initialReleaseYear={album?.releaseYear ?? new Date().getFullYear()}
		titlePlaceholder="Sin título"
		coverFallbackUrl={album ? albumCover(album.id, 'large') : undefined}
		coverHint="Las canciones del álbum usan esta portada."
		requireCover={isNew}
		{formMessage}
		submitLabel={isNew ? 'Crear álbum' : 'Guardar cambios'}
		submittingLabel={isNew ? 'Creando…' : 'Guardando…'}
		onCancel={onClose}
		onSuccess={saved}
	>
		{#snippet aside()}
			<input type="hidden" name="albumId" value={album?.id ?? ''} />
			<AlbumTrackPicker {tracks} {memberIds} onchange={() => (dirty = true)} />
		{/snippet}
		{#snippet footer()}
			{#if album}
				<Button variant="danger" size="sm" onclick={() => (confirmingDelete = true)}>
					Eliminar álbum
				</Button>
			{/if}
		{/snippet}
	</AlbumForm>
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
