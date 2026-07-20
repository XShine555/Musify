<script lang="ts">
	import type { SessionUser } from '$lib/types';
	import Upload from '@lucide/svelte/icons/upload';
	import Folder from '@lucide/svelte/icons/folder';
	import LogOut from '@lucide/svelte/icons/log-out';

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
	<a href="/" class="text-lg font-extrabold tracking-wide">Musify</a>

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
				<span
					class="grid h-9 w-9 place-items-center rounded-full bg-surface-2 text-sm font-semibold uppercase"
				>
					{user.name.charAt(0)}
				</span>
			{/if}
		</button>

		{#if menuOpen}
			<div
				class="animate-pop absolute top-full right-0 mt-2 w-56 overflow-hidden rounded-panel border border-line bg-elevated py-1.5 shadow-menu"
			>
				<a
					href="/library"
					onclick={() => (menuOpen = false)}
					class="flex w-full items-center gap-3 px-3 py-2.5 text-base font-semibold text-fg-2 transition hover:bg-hover"
				>
					<Folder class="h-[18px] w-[18px]" strokeWidth={2} />
					Canciones subidas
				</a>
				<a
					href="/upload"
					onclick={() => (menuOpen = false)}
					class="flex w-full items-center gap-3 px-3 py-2.5 text-base font-semibold text-fg-2 transition hover:bg-hover"
				>
					<Upload class="h-[18px] w-[18px]" strokeWidth={2} />
					Subir música
				</a>
				<div class="my-1.5 border-t border-line"></div>
				<form method="POST" action="/logout" data-sveltekit-reload>
					<button
						type="submit"
						class="flex w-full items-center gap-3 px-3 py-2.5 text-base font-semibold text-fg-2 transition hover:bg-hover"
					>
						<LogOut class="h-[18px] w-[18px]" strokeWidth={2} />
						Salir
					</button>
				</form>
			</div>
		{/if}
	</div>
</header>

<svelte:window onclick={onDocumentClick} />
