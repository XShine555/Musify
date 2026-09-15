<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogIn from '@lucide/svelte/icons/log-in';
	import MenuItem from './ui/MenuItem.svelte';
	import AccountMenu from './AccountMenu.svelte';

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
		<span
			class="h-5.5 w-5.5 rounded-[7px] bg-[image:var(--mf-logo-grad)] shadow-[var(--mf-logo-glow)]"
		></span>
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
						{#if user.picture}
							<img src={user.picture} alt="" class="h-9 w-9 rounded-full object-cover" />
						{:else}
							<span
								class="grid h-9 w-9 place-items-center rounded-full bg-surface-2 text-sm uppercase"
							>
								{user.name.charAt(0)}
							</span>
						{/if}
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
