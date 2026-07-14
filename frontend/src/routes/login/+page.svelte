<script lang="ts">
	let { data } = $props();

	const returnParam = $derived(`returnTo=${encodeURIComponent(data.returnTo)}`);
</script>

<svelte:head>
	<title>Iniciar sesión · Musify</title>
	<meta name="description" content="Inicia sesión o crea una cuenta en Musify." />
</svelte:head>

<div class="grid min-h-screen place-items-center bg-black px-6 text-[var(--mf-text)]">
	<div class="flex w-full max-w-[440px] flex-col items-center gap-9 text-center">
		<div class="font-display text-3xl font-extrabold tracking-tight">Musify</div>

		<div>
			<h1 class="font-display text-[36px] font-extrabold tracking-tight">Tu música. Sin límites.</h1>
			<p class="mt-3 text-base text-[var(--mf-text-2)]">Inicia sesión para continuar.</p>
		</div>

		<div class="flex w-full flex-col gap-5">
			<a
				href={`/auth/login?${returnParam}`}
				data-sveltekit-reload
				class="rounded-lg px-6 py-3.5 text-base font-semibold transition hover:brightness-110"
				style="background:var(--mf-accent);color:#000"
			>
				Iniciar Sesión
			</a>
			<a
				href={`/auth/login?mode=register&${returnParam}`}
				data-sveltekit-reload
				class="rounded-lg bg-[var(--mf-surface-2)] px-6 py-3.5 text-base font-semibold transition hover:bg-[var(--mf-surface-hover)]"
			>
				Crear Cuenta
			</a>
		</div>
	</div>
</div>
