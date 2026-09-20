<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import SquarePencil from '@lucide/svelte/icons/square-pen';
	import Trash from '@lucide/svelte/icons/trash';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
	import { plural } from '$lib/format';

	interface Props {
		title: string;
		description?: string;
		releaseYear?: number;
		trackIds: (string | number)[];
		coverUrl?: string;
		playing: boolean;
		isOwner: boolean;
		onPlayAll: () => void;
		onEdit: () => void;
		onDelete: () => void;
	}

	let {
		title,
		description,
		releaseYear,
		trackIds,
		coverUrl,
		playing,
		isOwner,
		onPlayAll,
		onEdit,
		onDelete
	}: Props = $props();

	const meta = $derived(
		[releaseYear ? String(releaseYear) : undefined, plural(trackIds.length, 'canción', 'canciones')]
			.filter((part) => part !== undefined)
			.join(' · ')
	);
</script>

<BackLink
	href={isOwner ? '/albums' : '/'}
	label={isOwner ? 'Volver a tus álbumes' : 'Volver al inicio'}
/>

<CollectionHeader eyebrow="Álbum" {title} {description} {meta}>
	{#snippet cover()}
		<Artwork src={coverUrl} {trackIds} size="hero" alt={title} class="shrink-0" />
	{/snippet}
	{#snippet actions()}
		<Button size="sm" onclick={onPlayAll} disabled={trackIds.length === 0}>
			{#if playing}
				<Pause class="h-4 w-4" strokeWidth={1.5} />
				Pausar
			{:else}
				<Play class="h-4 w-4" strokeWidth={1.5} />
				Reproducir
			{/if}
		</Button>
		{#if isOwner}
			<Button size="sm" variant="secondary" onclick={onEdit}>
				<SquarePencil class="h-4 w-4" strokeWidth={1.5} />
				Editar
			</Button>
			<Button size="sm" variant="secondary" onclick={onDelete}>
				<Trash class="h-4 w-4" strokeWidth={1.5} />
				Eliminar
			</Button>
		{/if}
	{/snippet}
</CollectionHeader>
