<script lang="ts">
	let { data } = $props();

	const tracks = $derived(data.tracks);
	const currentPage = $derived(Number(tracks.pageNumber));
	const total = $derived(Number(tracks.totalItemCount));

	const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

	function pageHref(page: number) {
		const params = new URLSearchParams();
		if (data.query) params.set('q', data.query);
		if (page > 1) params.set('page', String(page));
		const qs = params.toString();
		return qs ? `/explore?${qs}` : '/explore';
	}
</script>

<svelte:head>
	<title>Explorar · Musify</title>
	<meta name="description" content="Explora y busca canciones en Musify." />
</svelte:head>

<section class="mx-auto max-w-6xl px-6 py-12">
	<h1 class="text-3xl font-bold tracking-tight sm:text-4xl">Explorar</h1>
	<p class="mt-2 text-neutral-400">Descubre canciones o busca por nombre.</p>

	<form method="GET" class="mt-8 flex gap-3">
		<div class="relative flex-1">
			<span class="pointer-events-none absolute inset-y-0 left-4 grid place-items-center text-neutral-500"
				>🔍</span
			>
			<input
				type="search"
				name="q"
				value={data.query}
				placeholder="Buscar por nombre…"
				autocomplete="off"
				class="w-full rounded-full border border-white/10 bg-white/5 py-3 pl-11 pr-4 text-sm text-neutral-100 placeholder:text-neutral-500 focus:border-emerald-500/50 focus:outline-none"
			/>
		</div>
		<button
			type="submit"
			class="rounded-full bg-emerald-500 px-6 py-3 text-sm font-semibold text-neutral-950 transition hover:bg-emerald-400"
		>
			Buscar
		</button>
	</form>

	<p class="mt-6 text-sm text-neutral-500">
		{#if data.query}
			{total}
			{total === 1 ? 'resultado' : 'resultados'} para «{data.query}»
		{:else}
			{total}
			{total === 1 ? 'canción' : 'canciones'}
		{/if}
	</p>

	{#if tracks.items.length === 0}
		<div class="mt-16 rounded-2xl border border-white/5 bg-white/[0.03] p-12 text-center">
			<div class="text-4xl">🎵</div>
			<p class="mt-4 text-neutral-300">
				{#if data.query}
					No hay canciones que coincidan con «{data.query}».
				{:else}
					Todavía no hay canciones. ¡Sé el primero en subir una!
				{/if}
			</p>
		</div>
	{:else}
		<ul class="mt-8 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
			{#each tracks.items as track (track.id)}
				<li
					class="group rounded-2xl border border-white/5 bg-white/[0.03] p-4 transition hover:border-emerald-500/30 hover:bg-white/[0.05]"
				>
					<div
						class="grid aspect-square place-items-center rounded-xl bg-gradient-to-br from-emerald-500/20 to-emerald-500/5 text-4xl"
					>
						🎵
					</div>
					<h3 class="mt-3 truncate font-semibold" title={track.title}>{track.title}</h3>
					<p class="mt-1 text-xs text-neutral-500">
						{dateFormatter.format(new Date(track.createdAt))}
					</p>
				</li>
			{/each}
		</ul>

		{#if tracks.hasPreviousPage || tracks.hasNextPage}
			<nav class="mt-10 flex items-center justify-center gap-4">
				{#if tracks.hasPreviousPage}
					<a
						href={pageHref(currentPage - 1)}
						class="rounded-full border border-white/15 px-5 py-2 text-sm font-medium text-white transition hover:bg-white/5"
					>
						Anterior
					</a>
				{/if}
				<span class="text-sm text-neutral-500">Página {currentPage}</span>
				{#if tracks.hasNextPage}
					<a
						href={pageHref(currentPage + 1)}
						class="rounded-full border border-white/15 px-5 py-2 text-sm font-medium text-white transition hover:bg-white/5"
					>
						Siguiente
					</a>
				{/if}
			</nav>
		{/if}
	{/if}
</section>
