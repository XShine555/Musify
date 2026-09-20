<script lang="ts" module>
	import { MENU_WIDTH, SUBMENU_WIDTH } from '$lib/config';

	export interface MenuPosition {
		x: number;
		y: number;
		openLeft: boolean;
	}

	export function contextMenuPosition(event: MouseEvent): MenuPosition {
		event.preventDefault();
		return {
			x: Math.max(8, Math.min(event.clientX, window.innerWidth - MENU_WIDTH - 8)),
			y: Math.max(8, Math.min(event.clientY, window.innerHeight - 60)),
			openLeft: event.clientX + MENU_WIDTH + SUBMENU_WIDTH + 16 > window.innerWidth
		};
	}
</script>

<script lang="ts">
	import { enhance } from '$app/forms';
	import type { LucideIcon } from '@lucide/svelte';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import Artwork from './Artwork.svelte';
	import GlassMenu from './GlassMenu.svelte';
	import MenuItem from './MenuItem.svelte';

	interface Props {
		x: number;
		y: number;
		openLeft: boolean;
		onClose: () => void;
		items: { icon: LucideIcon; label: string; onclick: () => void }[];
		playlistAction?: { action: string; fields: Record<string, string>; label: string };
		playlists?: { id: string; name: string }[];
	}

	let { x, y, openLeft, onClose, items, playlistAction, playlists = [] }: Props = $props();

	function onWindowKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') onClose();
	}
</script>

<svelte:window onkeydown={onWindowKeydown} />

<div
	class="fixed inset-0 z-(--z-backdrop)"
	role="presentation"
	onclick={onClose}
	oncontextmenu={(e) => {
		e.preventDefault();
		onClose();
	}}
></div>
<GlassMenu class="fixed z-(--z-menu) w-(--mf-menu-w)" style="left:{x}px; top:{y}px;">
	{#each items as item (item.label)}
		<MenuItem icon={item.icon} label={item.label} onclick={item.onclick} />
	{/each}
	{#if playlistAction}
		<div class="group/addmenu relative">
			<div
				class="flex items-center gap-3 rounded-control px-2 py-2.5 text-sm font-medium text-fg-2 transition group-hover/addmenu:bg-hover"
			>
				<span class="flex-1 cursor-default">{playlistAction.label}</span>
				<ChevronRight class="size-icon-sm shrink-0 text-fg-3" />
			</div>
			<div
				class="invisible absolute top-0 z-(--z-submenu) opacity-0 transition group-hover/addmenu:visible group-hover/addmenu:opacity-100 max-sm:top-full max-sm:right-0 max-sm:left-0 max-sm:px-0 max-sm:pt-1.5 {openLeft
					? 'right-full pr-1.5'
					: 'left-full pl-1.5'}"
			>
				<div
					class="w-(--mf-submenu-w) overflow-hidden rounded-panel border border-line bg-elevated p-2 max-sm:w-full"
				>
					<div class="max-h-80 overflow-y-auto">
						{#if playlists.length === 0}
							<p class="px-2 py-2.5 text-sm text-fg-3">Todavía no tienes playlists.</p>
						{/if}
						{#each playlists as playlist (playlist.id)}
							<form
								method="POST"
								action={playlistAction.action}
								use:enhance={() =>
									({ update }) => {
										onClose();
										return update({ reset: false, invalidateAll: false });
									}}
							>
								<input type="hidden" name="playlistId" value={playlist.id} />
								{#each Object.entries(playlistAction.fields) as [name, value] (name)}
									<input type="hidden" {name} {value} />
								{/each}
								<button
									type="submit"
									class="flex w-full items-center gap-3 rounded-control px-2 py-2.5 text-left text-sm font-medium text-fg transition hover:bg-hover"
								>
									<Artwork
										src="/api/playlists/{playlist.id}/cover?size=small"
										size="xs"
										class="shrink-0"
									/>
									<span class="truncate text-fg-2">{playlist.name}</span>
								</button>
							</form>
						{/each}
					</div>
				</div>
			</div>
		</div>
	{/if}
</GlassMenu>
