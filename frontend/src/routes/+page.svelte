<script lang="ts">
	const features = [
		{
			icon: '🎧',
			title: 'Escucha sin límites',
			body: 'Reproducción con streaming adaptativo y soporte de peticiones por rango.'
		},
		{
			icon: '🎵',
			title: 'Tus playlists',
			body: 'Crea, organiza y comparte tus listas de reproducción a tu manera.'
		},
		{
			icon: '⬆️',
			title: 'Sube tu música',
			body: 'Sube tus canciones y portadas; nosotros nos encargamos de procesarlas.'
		}
	];
</script>

<svelte:head>
	<title>Musify · Tu música, en streaming</title>
	<meta name="description" content="Escucha, organiza y sube tu música con Musify." />
</svelte:head>

<section class="relative overflow-hidden">
	<div
		class="pointer-events-none absolute -top-40 left-1/2 h-96 w-[40rem] -translate-x-1/2 rounded-full bg-emerald-500/20 blur-3xl"
	></div>

	<div class="mx-auto max-w-6xl px-6 py-24 text-center sm:py-32">
		<span
			class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-4 py-1.5 text-xs font-medium text-neutral-300"
		>
			<span class="h-2 w-2 rounded-full bg-emerald-400"></span> Ahora en beta
		</span>

		<h1 class="mx-auto mt-6 max-w-3xl text-5xl font-bold tracking-tight sm:text-6xl">
			Tu música, <span class="text-emerald-400">en streaming</span>.
		</h1>

		<p class="mx-auto mt-6 max-w-xl text-lg text-neutral-400">
			Escucha, organiza y sube tus canciones. Musify es tu biblioteca musical, siempre contigo.
		</p>

		<div class="mt-10 flex flex-wrap items-center justify-center gap-4">
			<a
				href="/login"
				class="rounded-full bg-emerald-500 px-6 py-3 text-sm font-semibold text-neutral-950 transition hover:bg-emerald-400"
			>
				Empezar gratis
			</a>
			<a
				href="/explore"
				class="rounded-full border border-white/15 px-6 py-3 text-sm font-semibold text-white transition hover:bg-white/5"
			>
				Explorar canciones
			</a>
		</div>
	</div>
</section>

<section class="mx-auto max-w-6xl px-6 pb-24">
	<div class="grid gap-6 sm:grid-cols-3">
		{#each features as feature (feature.title)}
			<div
				class="rounded-2xl border border-white/5 bg-white/[0.03] p-6 transition hover:border-emerald-500/30 hover:bg-white/[0.05]"
			>
				<div class="text-3xl">{feature.icon}</div>
				<h3 class="mt-4 text-lg font-semibold">{feature.title}</h3>
				<p class="mt-2 text-sm text-neutral-400">{feature.body}</p>
			</div>
		{/each}
	</div>
</section>
