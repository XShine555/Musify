<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogIn from '@lucide/svelte/icons/log-in';
	import MenuItem from '$lib/components/ui/overlay/MenuItem.svelte';
	import AccountMenu from '$lib/components/ui/overlay/AccountMenu.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import Logo from '$lib/components/ui/primitives/Logo.svelte';

	interface Props {
		user: SessionUser | null;
		accountUrl?: string | null;
	}

	let { user, accountUrl }: Props = $props();
</script>

<header
	class="sticky top-0 z-(--z-sticky) flex h-14 items-center justify-between bg-transparent page-x backdrop-blur-md sm:hidden"
>
	<a href="/">
		<Logo size="sm" />
	</a>

	{#if user}
		<AccountMenu {user} {accountUrl} width="md">
			{#snippet extraItems({ close })}
				<MenuItem icon={Folder} label="Canciones subidas" href="/library" onclick={close} />
				<MenuItem icon={Upload} label="Subir música" href="/upload" onclick={close} />
			{/snippet}
		</AccountMenu>
	{:else}
		<IconButton href="/auth/login" reload label="Iniciar sesión" shape="round" size="md">
			<LogIn class="size-icon-lg" />
		</IconButton>
	{/if}
</header>
