<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogIn from '@lucide/svelte/icons/log-in';
	import MenuItem from '../ui/overlay/MenuItem.svelte';
	import AccountMenu from '../ui/overlay/AccountMenu.svelte';
	import Avatar from '../ui/media/Avatar.svelte';
	import IconButton from '../ui/primitives/IconButton.svelte';
	import Logo from '../ui/primitives/Logo.svelte';

	interface Props {
		user: SessionUser | null;
		accountUrl?: string | null;
	}

	let { user, accountUrl }: Props = $props();
</script>

<header
	class="sticky top-0 z-(--z-sticky) flex h-14 items-center justify-between border-b border-hairline bg-bg/85 page-x backdrop-blur-md sm:hidden"
>
	<a href="/">
		<Logo size="sm" />
	</a>

	{#if user}
		<AccountMenu {user} {accountUrl} width="md">
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
		<IconButton href="/auth/login" reload label="Iniciar sesión" shape="round" size="md">
			<LogIn class="size-icon-lg" strokeWidth={2} />
		</IconButton>
	{/if}
</header>
