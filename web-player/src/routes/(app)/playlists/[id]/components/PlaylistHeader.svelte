<script lang="ts">
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';

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
		`${trackIds.length} ${trackIds.length === 1 ? 'canción' : 'canciones'} · ${
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
		<PlaylistArt
			playlistId={id}
			{trackIds}
			size="large"
			version={updatedAt}
			class="h-36 w-36 shrink-0 rounded-[18px] shadow-cover-lg sm:h-41 sm:w-41"
		/>
	{/snippet}
	{#snippet actions()}
		<Button size="sm" onclick={onPlayAll} disabled={trackIds.length === 0}>
			{#if playing}
				Pausar
			{:else}
				Reproducir
			{/if}
		</Button>
		<Button size="sm" variant="secondary" onclick={onEdit}>Editar</Button>
		<Button size="sm" variant="secondary" onclick={onDelete}>Eliminar</Button>
	{/snippet}
</CollectionHeader>
