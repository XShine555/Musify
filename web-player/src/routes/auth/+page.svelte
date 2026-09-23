<script lang="ts">
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import Logo from '$lib/components/ui/primitives/Logo.svelte';

	let { data } = $props();

	const returnParam = $derived(`returnTo=${encodeURIComponent(data.returnTo)}`);
</script>

<svelte:head>
	<title>Iniciar sesión</title>
	<meta name="description" content="Inicia sesión o crea una cuenta en Musify." />
</svelte:head>

<div class="relative grid min-h-screen place-items-center overflow-hidden bg-bg px-6 text-fg">
	<div class="pointer-events-none absolute inset-0" style="background:var(--mf-ambient)"></div>

	<div class="relative flex w-full max-w-4xl flex-col items-center gap-12 text-center">
		<Logo size="lg" />

		<div class="animate-enter flex flex-col items-center gap-5">
			<h1
				class="font-display text-[clamp(2.75rem,9vw,8rem)] leading-[1.05] font-semibold tracking-tight"
			>
				<span class="block">Tu música.</span>
				<span class="block">Sin límites.</span>
			</h1>
			<p class="text-lg text-fg-2">Inicia sesión para continuar.</p>
		</div>

		<div class="flex w-full max-w-cta flex-col gap-3">
			<Button href={`/auth/login?${returnParam}`} reload size="lg" class="w-full">
				Iniciar sesión
			</Button>
			<Button
				href={`/auth/login?mode=register&${returnParam}`}
				reload
				variant="secondary"
				size="lg"
				class="w-full"
			>
				Crear cuenta
			</Button>
		</div>
	</div>
</div>
