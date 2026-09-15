<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogOut from '@lucide/svelte/icons/log-out';
	import LogIn from '@lucide/svelte/icons/log-in';
	import User from '@lucide/svelte/icons/user';
	import Settings from '@lucide/svelte/icons/settings';
	import Sun from '@lucide/svelte/icons/sun';
	import Moon from '@lucide/svelte/icons/moon';
	import MenuItem from './ui/MenuItem.svelte';
	import GlassMenu from './ui/GlassMenu.svelte';
	import { themeMode } from '$lib/theme/mode.svelte';

	interface Props {
		user: SessionUser | null;
		accountUrl?: string | null;
	}

	let { user, accountUrl }: Props = $props();

	let menuOpen = $state(false);
	let menuRef: HTMLDivElement | undefined = $state();

	function onDocumentClick(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) menuOpen = false;
	}
</script>

<header
	class="sticky top-0 z-40 flex h-14 items-center justify-between border-b border-hairline bg-bg/85 page-x backdrop-blur-md sm:hidden"
>
	<a
		href="/"
		class="flex items-center gap-2 font-display text-[17.5px] font-semibold tracking-[-0.02em] text-fg"
	>
		<span
			class="h-5.5 w-5.5 rounded-[7px] bg-[image:var(--mf-logo-grad)] shadow-[var(--mf-logo-glow)]"
		></span>
		Musify
	</a>

	<div class="relative" bind:this={menuRef}>
		{#if user}
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
				<GlassMenu class="absolute top-full right-0 mt-2 w-56 overflow-hidden">
					<MenuItem
						icon={User}
						label="Ver perfil"
						href="/u/{user.sub}"
						onclick={() => (menuOpen = false)}
					/>
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
					{#if accountUrl}
						<MenuItem
							icon={Settings}
							label="Ajustes"
							href={accountUrl}
							target="_blank"
							onclick={() => (menuOpen = false)}
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
		{:else}
			<a
				href="/auth/login"
				data-sveltekit-reload
				aria-label="Iniciar sesión"
				class="flex items-center rounded-full p-1.5 text-fg-3 transition hover:bg-hover hover:text-fg"
			>
				<LogIn class="h-6 w-6" strokeWidth={2} />
			</a>
		{/if}
	</div>
</header>

<svelte:window onclick={onDocumentClick} />
