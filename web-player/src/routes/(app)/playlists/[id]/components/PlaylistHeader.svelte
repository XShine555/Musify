<script lang="ts">
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
	import { plural } from '$lib/format';

	interface Props {
		id: string;
		name: string;
		description?: string | null;
		trackIds: (string | number)[];
		updatedAt: string;
		visibility: 'Public' | 'Private';
		playing: boolean;
		onPlayAll: () => void;
		onEdit: () => void;
		onDelete: () => void;
	}

	let {
		id,
		name,
		description,
		trackIds,
		updatedAt,
		visibility,
		playing,
		onPlayAll,
		onEdit,
		onDelete
	}: Props = $props();

	const meta = $derived(
		`${plural(trackIds.length, 'canción', 'canciones')} · ${
			visibility === 'Public' ? 'Pública' : 'Privada'
		}`
	);
</script>

<CollectionHeader
	eyebrow="Lista"
	title={name}
	description={description ?? undefined}
	{meta}
	align="center"
>
	{#snippet cover()}
		<Artwork
			src="/api/playlists/{id}/cover?size=large&v={encodeURIComponent(updatedAt)}"
			{trackIds}
			size="hero"
			alt={name}
			class="shrink-0"
		/>
	{/snippet}
	{#snippet actions()}
		<Button size="sm" onclick={onPlayAll} disabled={trackIds.length === 0}>
			{playing ? 'Pausar' : 'Reproducir'}
		</Button>
		<Button size="sm" variant="secondary" onclick={onEdit}>Editar</Button>
		<Button size="sm" variant="secondary" onclick={onDelete}>Eliminar</Button>
	{/snippet}
</CollectionHeader>
