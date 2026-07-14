<script lang="ts">
	import type { SessionUser } from '$lib/types';

	interface Props {
		user: SessionUser | null;
	}

	let { user }: Props = $props();
</script>

<header class="sticky top-0 z-50 border-b border-white/10 bg-neutral-950/80 backdrop-blur">
	<nav class="flex items-center justify-between gap-4 px-8 py-4">
		<a href="/" class="text-lg tracking-tight text-neutral-100 transition hover:text-white">
			Musify
		</a>

		<div class="flex items-center gap-6">
			{#if user}
				<a href="/explore" class="flex items-center gap-2 text-sm text-neutral-200">
					{#if user.picture}
						<img src={user.picture} alt="" class="h-7 w-7 rounded-full object-cover" />
					{:else}
						<span
							class="grid h-7 w-7 place-items-center rounded-full bg-neutral-700 text-xs font-semibold uppercase"
						>
							{user.name.charAt(0)}
						</span>
					{/if}
					<span class="hidden sm:inline">{user.name}</span>
				</a>
				<form method="POST" action="/logout">
					<button
						class="rounded-full border border-white/20 px-4 py-2 text-sm font-semibold text-white backdrop-blur-md transition hover:bg-white/5"
					>
						Salir
					</button>
				</form>
			{:else}
				<a
					href="/login"
					data-sveltekit-reload
					class="text-sm font-medium text-neutral-300 transition hover:text-white"
				>
					Iniciar sesión
				</a>
				<a
					href="/login?mode=register"
					data-sveltekit-reload
					class="rounded-full bg-[var(--mf-accent)] px-4 py-2 text-sm font-semibold text-neutral-950 backdrop-blur-md transition hover:brightness-110"
				>
					Registrarse
				</a>
			{/if}
		</div>
	</nav>
</header>
