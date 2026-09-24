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
	import { clickOutside } from '$lib/actions/clickOutside';
	import { onEscape } from '$lib/actions/onEscape';
	import Avatar from '$lib/components/ui/media/Avatar.svelte';

	interface Props {
		user: SessionUser;
		accountUrl?: string | null;
		width?: 'sm' | 'md';
		extraItems?: Snippet<[{ close: () => void }]>;
	}

	let { user, accountUrl, width = 'sm', extraItems }: Props = $props();

	const WIDTH: Record<'sm' | 'md', string> = { sm: 'w-52', md: 'w-56' };

	let open = $state(false);

	function toggle() {
		open = !open;
	}

	function close() {
		open = false;
	}
</script>

<div class="relative" use:clickOutside={close} use:onEscape={open ? close : undefined}>
	<button
		type="button"
		onclick={toggle}
		aria-label="Tu cuenta"
		aria-expanded={open}
		class="block shrink-0 rounded-full transition active:scale-95"
	>
		<Avatar
			name={user.name}
			src={user.picture}
			size="sm"
			class="border border-line-strong opacity-90"
		/>
	</button>

	{#if open}
		<GlassMenu
			border={false}
			class="absolute top-full right-0 z-(--z-menu) mt-2 overflow-hidden {WIDTH[width]}"
		>
			<MenuItem icon={User} label="Ver perfil" href="/user/{user.sub}" onclick={close} />
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
				label={themeMode.current === 'dark' ? 'Modo claro' : 'Modo oscuro'}
				onclick={() => themeMode.toggle()}
			/>
			<form method="POST" action="/auth/logout" data-sveltekit-reload>
				<MenuItem icon={LogOut} label="Cerrar sesión" type="submit" />
			</form>
		</GlassMenu>
	{/if}
</div>
