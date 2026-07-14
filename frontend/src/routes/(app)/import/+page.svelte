<script lang="ts">
	import Infinity_ from '@lucide/svelte/icons/infinity';
</script>

<svelte:head>
	<title>Importar · Musify</title>
	<meta name="description" content="Importa tu música desde otros servicios." />
</svelte:head>

<section class="px-8 py-11">
	<h1 class="font-display text-[28px] font-extrabold tracking-tight">Importar</h1>
	<p class="mb-7 mt-1.5 max-w-[560px] text-sm text-[var(--mf-text-2)]">
		Trae tu música desde otros servicios.
	</p>

	<div
		class="max-w-[560px] rounded-xl border border-[var(--mf-border)] bg-[var(--mf-surface)] px-6 py-10 text-center"
	>
		<Infinity_ class="mx-auto h-8 w-8 text-[var(--mf-text-3)]" strokeWidth={1.5} />
		<p class="mt-4 text-sm text-[var(--mf-text-2)]">Próximamente</p>
		<p class="mt-1.5 text-[12.5px] text-[var(--mf-text-4)]">
			Podrás importar tu música desde otras plataformas.
		</p>
	</div>
</section>
