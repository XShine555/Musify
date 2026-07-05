<script lang="ts">
	import type { SessionUser } from '$lib/types';

	interface Props {
		user: SessionUser | null;
	}

	let { user }: Props = $props();

	const links = [
		{ href: '/', label: 'Inicio' },
		{ href: '/explore', label: 'Explorar' }
	];
</script>

<header class="sticky top-0 z-50 border-b border-white/5 bg-neutral-950/80 backdrop-blur">
	<nav class="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
		<a href="/" class="flex items-center gap-2 text-lg font-bold tracking-tight">
			<span class="grid h-8 w-8 place-items-center rounded-lg bg-emerald-500 text-neutral-950">♪</span>
			Musify
		</a>

		<div class="hidden items-center gap-8 md:flex">
			{#each links as link (link.href)}
				<a href={link.href} class="text-sm text-neutral-300 transition hover:text-white">{link.label}</a>
			{/each}
		</div>

		<div class="flex items-center gap-3">
			{#if user}
				<a href="/explore" class="flex items-center gap-2 text-sm text-neutral-200">
					{#if user.picture}
						<img src={user.picture} alt="" class="h-7 w-7 rounded-full object-cover" />
					{:else}
						<span
							class="grid h-7 w-7 place-items-center rounded-full bg-neutral-700 text-xs font-semibold uppercase"
						>
							{(user.name ?? user.email ?? '?').charAt(0)}
						</span>
					{/if}
					<span class="hidden sm:inline">{user.name ?? user.email}</span>
				</a>
				<form method="POST" action="/logout">
					<button
						class="rounded-full border border-white/15 px-4 py-2 text-sm font-semibold text-white transition hover:bg-white/5"
					>
						Salir
					</button>
				</form>
			{:else}
				<a href="/login" class="text-sm font-medium text-neutral-300 transition hover:text-white">
					Iniciar sesión
				</a>
				<a
					href="/login?mode=register"
					class="rounded-full bg-emerald-500 px-4 py-2 text-sm font-semibold text-neutral-950 transition hover:bg-emerald-400"
				>
					Registrarse
				</a>
			{/if}
		</div>
	</nav>
</header>
