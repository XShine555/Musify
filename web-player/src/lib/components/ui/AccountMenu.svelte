<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { SessionUser } from '$lib/types';
	import LogOut from '@lucide/svelte/icons/log-out';
	import User from '@lucide/svelte/icons/user';
	import Settings from '@lucide/svelte/icons/settings';
	import Sun from '@lucide/svelte/icons/sun';
	import Moon from '@lucide/svelte/icons/moon';
	import MenuItem from './MenuItem.svelte';
	import GlassMenu from './GlassMenu.svelte';
	import { themeMode } from '$lib/theme/mode.svelte';

	interface Props {
		user: SessionUser;
		accountUrl?: string | null;
		panelClass?: string;
		extraItems?: Snippet<[{ close: () => void }]>;
		trigger: Snippet<[{ toggle: () => void; open: boolean }]>;
	}

	let {
		user,
		accountUrl,
		panelClass = 'absolute top-full right-0 mt-2 w-52 overflow-hidden',
		extraItems,
		trigger
	}: Props = $props();

	let open = $state(false);
	let menuRef: HTMLDivElement | undefined = $state();

	function toggle() {
		open = !open;
	}

	function close() {
		open = false;
	}

	function onDocumentClick(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) open = false;
	}
</script>

<div class="relative" bind:this={menuRef}>
	{@render trigger({ toggle, open })}

	{#if open}
		<GlassMenu class={panelClass}>
			<MenuItem icon={User} label="Ver perfil" href="/u/{user.sub}" onclick={close} />
			{@render extraItems?.({ close })}
			{#if accountUrl}
				<MenuItem
					icon={Settings}
					label="Ajustes"
					href={accountUrl}
					target="_blank"
					onclick={close}
				/>
			{/if}
			<MenuItem
				icon={themeMode.current === 'dark' ? Sun : Moon}
				label={themeMode.current === 'dark' ? 'Modo blanco' : 'Modo oscuro'}
				onclick={() => themeMode.toggle()}
			/>
			<form method="POST" action="/logout" data-sveltekit-reload>
				<MenuItem icon={LogOut} label="Cerrar sesión" type="submit" />
			</form>
		</GlassMenu>
	{/if}
</div>

<svelte:window onclick={onDocumentClick} />
