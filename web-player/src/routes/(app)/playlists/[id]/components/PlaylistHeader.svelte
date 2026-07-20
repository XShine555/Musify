<script lang="ts">
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import SquarePencil from '@lucide/svelte/icons/square-pen';
	import Trash from '@lucide/svelte/icons/trash';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { hueFor } from '$lib/theme/color';

	interface Props {
		id: string;
		name: string;
		description?: string;
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

<a
	href="/playlists"
	class="group inline-flex items-center text-fg-3 transition-colors hover:text-accent-soft"
>
	<span class="grid h-8 w-8 place-items-center transition-colors group-hover:text-accent-soft">
		<ArrowLeft class="h-4 w-4" />
	</span>
	Volver a tus listas
</a>

<div class="mt-6 flex flex-col gap-6 sm:flex-row sm:items-end">
	<PlaylistArt
		playlistId={id}
		{trackIds}
		hue={hueFor(id)}
		size="large"
		version={updatedAt}
		class="h-44 w-44 flex-shrink-0 rounded-art-lg shadow-art-lg"
	/>
	<div class="min-w-0 flex-1">
		<p class="text-sm tracking-[0.14em] text-fg-3 uppercase">Lista</p>
		<h1 class="mt-1.5 text-5xl font-bold tracking-tight text-fg sm:text-7xl">{name}</h1>
		{#if description}
			<p class="mt-3 max-w-2xl text-fg-2">{description}</p>
		{/if}
		<p class="mt-3 text-sm text-muted">
			{trackIds.length}
			{trackIds.length === 1 ? 'canción' : 'canciones'}
		</p>
	</div>
</div>

<div class="mt-7 flex items-center gap-2.5">
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
