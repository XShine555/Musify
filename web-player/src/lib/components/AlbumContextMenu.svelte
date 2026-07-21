<script lang="ts" module>
	const MENU_WIDTH = 240;
	const SUBMENU_WIDTH = 256;

	export type AlbumMenuState = { x: number; y: number; openLeft: boolean } & (
		{ kind: 'local'; albumId: string } | { kind: 'youtube'; albumId: string }
	);

	export function albumContextMenuStateFor(
		event: MouseEvent,
		kind: 'local' | 'youtube',
		albumId: string
	): AlbumMenuState {
		event.preventDefault();
		return {
			kind,
			albumId,
			x: Math.max(8, Math.min(event.clientX, window.innerWidth - MENU_WIDTH - 8)),
			y: Math.max(8, Math.min(event.clientY, window.innerHeight - 60)),
			openLeft: event.clientX + MENU_WIDTH + SUBMENU_WIDTH + 16 > window.innerWidth
		};
	}
</script>

<script lang="ts">
	import { enhance } from '$app/forms';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import { hueFor } from '$lib/theme/color';

	interface Props {
		menu: AlbumMenuState;
		playlists: { id: string; name: string }[];
		onClose: () => void;
	}

	let { menu, playlists, onClose }: Props = $props();

	function onWindowKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') onClose();
	}
</script>

<svelte:window onkeydown={onWindowKeydown} />

<div
	class="fixed inset-0 z-30"
	role="presentation"
	onclick={onClose}
	oncontextmenu={(e) => {
		e.preventDefault();
		onClose();
	}}
></div>
<div
	class="fixed z-40 w-60 rounded-panel border border-line bg-elevated p-1.5 shadow-menu"
	style="left:{menu.x}px; top:{menu.y}px;"
>
	{#if playlists.length > 0}
		<div class="group/addmenu relative">
			<div
				class="flex items-center gap-3 rounded-control px-2 py-2.5 text-sm text-fg-2 transition group-hover/addmenu:bg-hover"
			>
				<span class="flex-1 cursor-default text-base">Añadir álbum a una playlist</span>
				<ChevronRight class="h-4 w-4 shrink-0 text-fg-3" />
			</div>
			<div
				class="invisible absolute top-0 z-50 opacity-0 transition group-hover/addmenu:visible group-hover/addmenu:opacity-100 max-sm:top-full max-sm:right-0 max-sm:left-0 max-sm:px-0 max-sm:pt-1.5 {menu.openLeft
					? 'right-full pr-1.5'
					: 'left-full pl-1.5'}"
			>
				<div
					class="w-64 overflow-hidden rounded-panel border border-line bg-elevated p-2 shadow-menu max-sm:w-full"
				>
					<div class="max-h-80 overflow-y-auto">
						{#each playlists as playlist (playlist.id)}
							<form
								method="POST"
								action={menu.kind === 'youtube'
									? '?/addYoutubeAlbumToPlaylist'
									: '?/addAlbumToPlaylist'}
								use:enhance={() =>
									({ update }) => {
										onClose();
										return update({ reset: false, invalidateAll: false });
									}}
							>
								<input type="hidden" name="playlistId" value={playlist.id} />
								<input type="hidden" name="albumId" value={menu.albumId} />
								<button
									type="submit"
									class="flex w-full items-center gap-3 rounded-control px-2 py-2.5 text-left text-base text-fg transition hover:bg-hover"
								>
									<PlaylistArt
										playlistId={playlist.id}
										trackIds={[]}
										hue={hueFor(playlist.id)}
										size="small"
										class="h-9 w-9 shrink-0 rounded-control"
									/>
									<span class="truncate text-fg-2">{playlist.name}</span>
								</button>
							</form>
						{/each}
					</div>
				</div>
			</div>
		</div>
	{:else}
		<p class="px-2 py-2.5 text-sm text-fg-3">No tienes playlists todavía.</p>
	{/if}
</div>
