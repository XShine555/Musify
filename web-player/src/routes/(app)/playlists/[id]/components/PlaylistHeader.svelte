<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import SquarePencil from '@lucide/svelte/icons/square-pen';
	import Trash from '@lucide/svelte/icons/trash';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	interface Props {
		id: string;
		name: string;
		description?: string | null;
		trackIds: (string | number)[];
		updatedAt: string;
		playing: boolean;
		onPlayAll: () => void;
		onEdit: () => void;
		onDelete: () => void;
	}

	let { id, name, description, trackIds, updatedAt, playing, onPlayAll, onEdit, onDelete }: Props =
		$props();
</script>

<BackLink href="/playlists" label="Volver a tus listas" />

<div class="mt-5 flex flex-col gap-5 sm:mt-6 sm:flex-row sm:items-end sm:gap-6">
	<PlaylistArt
		playlistId={id}
		{trackIds}
		size="large"
		version={updatedAt}
		class="h-36 w-36 shrink-0 rounded-art-lg shadow-art-lg sm:h-44 sm:w-44"
	/>
	<div class="min-w-0 flex-1">
		<p class="text-sm tracking-[0.14em] text-fg-3 uppercase">Lista</p>
		<h1
			class="mt-1.5 text-3xl font-semibold tracking-tight break-words text-fg sm:text-5xl md:text-7xl"
		>
			{name}
		</h1>
		{#if description}
			<p class="mt-3 max-w-2xl text-sm text-fg-2 sm:text-base">{description}</p>
		{/if}
		<p class="mt-3 text-sm text-muted">
			{trackIds.length}
			{trackIds.length === 1 ? 'canción' : 'canciones'}
		</p>
	</div>
</div>

<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
	<Button size="sm" onclick={onPlayAll} disabled={trackIds.length === 0}>
		{#if playing}
			<Pause class="h-4 w-4" fill="currentColor" strokeWidth={1.5} />
			Pausar
		{:else}
			<Play class="h-4 w-4" fill="currentColor" strokeWidth={1.5} />
			Reproducir
		{/if}
	</Button>
	<Button size="sm" variant="secondary" onclick={onEdit}>
		<SquarePencil class="h-4 w-4" strokeWidth={1.5} />
		Editar
	</Button>
	<Button size="sm" variant="secondary" onclick={onDelete}>
		<Trash class="h-4 w-4" strokeWidth={1.5} />
		Eliminar
	</Button>
</div>
