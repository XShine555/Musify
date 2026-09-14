<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import SquarePencil from '@lucide/svelte/icons/square-pen';
	import Trash from '@lucide/svelte/icons/trash';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import Button from '$lib/components/ui/Button.svelte';

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
</script>

<div class="flex flex-col gap-5 sm:flex-row sm:items-center sm:gap-6">
	<PlaylistArt
		playlistId={id}
		{trackIds}
		size="large"
		version={updatedAt}
		class="h-36 w-36 shrink-0 rounded-[18px] shadow-[0_24px_56px_rgba(0,0,0,.55)] sm:h-41 sm:w-41"
	/>
	<div class="min-w-0 flex-1">
		<p class="text-[10.5px] font-semibold tracking-[0.16em] text-fg-2 uppercase">Lista</p>
		<h1
			class="mt-3 font-display text-[28px] font-semibold tracking-[-0.035em] break-words text-fg sm:text-[33px]"
		>
			{name}
		</h1>
		{#if description}
			<p class="mt-3 max-w-2xl text-sm text-fg-2 sm:text-base">{description}</p>
		{/if}
		<p class="mt-3.25 text-[12.5px] text-fg-2">
			{trackIds.length}
			{trackIds.length === 1 ? 'canción' : 'canciones'} · {visibility === 'Public'
				? 'Pública'
				: 'Privada'}
		</p>
	</div>
</div>

<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
	<Button size="sm" onclick={onPlayAll} disabled={trackIds.length === 0}>
		{#if playing}
			Pausar
		{:else}
			Reproducir
		{/if}
	</Button>
	<Button size="sm" variant="secondary" onclick={onEdit}>
		Editar
	</Button>
	<Button size="sm" variant="secondary" onclick={onDelete}>
		Eliminar
	</Button>
</div>
