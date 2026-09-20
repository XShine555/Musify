<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogIn from '@lucide/svelte/icons/log-in';
	import MenuItem from './ui/MenuItem.svelte';
	import AccountMenu from './ui/AccountMenu.svelte';
	import Avatar from './ui/Avatar.svelte';

	interface Props {
		user: SessionUser | null;
		accountUrl?: string | null;
	}

	let { user, accountUrl }: Props = $props();
</script>

<header
	class="sticky top-0 z-40 flex h-14 items-center justify-between border-b border-hairline bg-bg/85 page-x backdrop-blur-md sm:hidden"
>
	<a
		href="/"
		class="flex items-center gap-2 font-display text-lg font-semibold tracking-[-0.02em] text-fg"
	>
		<span class="h-5.5 w-5.5 rounded-lg bg-[image:var(--mf-logo-grad)]"></span>
		Musify
	</a>

	<div class="relative">
		{#if user}
			<AccountMenu
				{user}
				{accountUrl}
				panelClass="absolute top-full right-0 mt-2 w-56 overflow-hidden"
			>
				{#snippet trigger({ toggle, open })}
					<button
						type="button"
						onclick={toggle}
						aria-label="Tu cuenta"
						aria-expanded={open}
						class="flex items-center rounded-full"
					>
						<Avatar name={user.name} src={user.picture} size="sm" />
					</button>
				{/snippet}
				{#snippet extraItems({ close })}
					<MenuItem icon={Folder} label="Canciones subidas" href="/library" onclick={close} />
					<MenuItem icon={Upload} label="Subir música" href="/upload" onclick={close} />
				{/snippet}
			</AccountMenu>
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
