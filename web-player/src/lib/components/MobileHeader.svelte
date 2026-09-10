<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogOut from '@lucide/svelte/icons/log-out';
	import MenuItem from './ui/MenuItem.svelte';

	interface Props {
		user: SessionUser;
	}

	let { user }: Props = $props();

	let menuOpen = $state(false);
	let menuRef: HTMLDivElement | undefined = $state();

	function onDocumentClick(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) menuOpen = false;
	}
</script>

<header
	class="sticky top-0 z-40 flex h-14 items-center justify-between border-b border-line bg-bg/85 page-x backdrop-blur-md md:hidden"
>
	<a href="/" class="font-display text-lg font-semibold tracking-tight text-fg">Musify</a>

	<div class="relative" bind:this={menuRef}>
		<button
			type="button"
			onclick={() => (menuOpen = !menuOpen)}
			aria-label="Tu cuenta"
			aria-expanded={menuOpen}
			class="flex items-center rounded-full"
		>
			{#if user.picture}
				<img src={user.picture} alt="" class="h-9 w-9 rounded-full object-cover" />
			{:else}
				<span class="grid h-9 w-9 place-items-center rounded-full bg-surface-2 text-sm uppercase">
					{user.name.charAt(0)}
				</span>
			{/if}
		</button>

		{#if menuOpen}
			<div
				class="animate-pop absolute top-full right-0 mt-2 w-56 overflow-hidden rounded-panel border border-line bg-elevated p-1.5 shadow-menu"
			>
				<MenuItem
					icon={Folder}
					label="Canciones subidas"
					href="/library"
					onclick={() => (menuOpen = false)}
				/>
				<MenuItem
					icon={Upload}
					label="Subir música"
					href="/upload"
					onclick={() => (menuOpen = false)}
				/>
				<div class="-mx-1.5 my-1.5 border-t border-line"></div>
				<form method="POST" action="/logout" data-sveltekit-reload>
					<MenuItem icon={LogOut} label="Salir" type="submit" />
				</form>
			</div>
		{/if}
	</div>
</header>

<svelte:window onclick={onDocumentClick} />
