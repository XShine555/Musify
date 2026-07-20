<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';

	let { data } = $props();

	const returnParam = $derived(`returnTo=${encodeURIComponent(data.returnTo)}`);
</script>

<svelte:head>
	<title>Iniciar sesión · Musify</title>
	<meta name="description" content="Inicia sesión o crea una cuenta en Musify." />
</svelte:head>

<div class="grid min-h-screen place-items-center bg-bg px-6 text-fg">
	<div class="flex w-full max-w-110 flex-col items-center gap-9 text-center">
		<div class="text-3xl font-extrabold tracking-tight">Musify</div>

		<div>
			<h1 class="text-4xl font-extrabold tracking-tight">Tu música. Sin límites.</h1>
			<p class="mt-3 text-base text-fg-2">Inicia sesión para continuar.</p>
		</div>

		<div class="flex w-full flex-col gap-5">
			<Button href={`/auth/login?${returnParam}`} reload size="lg" class="w-full">
				Iniciar Sesión
			</Button>
			<Button
				href={`/auth/login?mode=register&${returnParam}`}
				reload
				variant="subtle"
				size="lg"
				class="w-full"
			>
				Crear Cuenta
			</Button>
		</div>
	</div>
</div>
