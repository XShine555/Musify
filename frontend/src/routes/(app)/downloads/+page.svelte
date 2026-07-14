<script lang="ts">
	import ArrowDown from '@lucide/svelte/icons/arrow-down';
</script>

<svelte:head>
	<title>Descargas · Musify</title>
	<meta name="description" content="Tus descargas en Musify." />
</svelte:head>

<section class="px-8 py-11">
	<h1 class="font-display text-[28px] font-extrabold tracking-tight">Descargas</h1>
	<p class="mb-7 mt-1.5 max-w-[560px] text-sm text-[var(--mf-text-2)]">
		Escucha sin conexión lo que descargues.
	</p>

	<div
		class="max-w-[560px] rounded-xl border border-[var(--mf-border)] bg-[var(--mf-surface)] px-6 py-10 text-center"
	>
		<ArrowDown class="mx-auto h-8 w-8 text-[var(--mf-text-3)]" strokeWidth={1.5} />
		<p class="mt-4 text-sm text-[var(--mf-text-2)]">Todavía no tienes descargas</p>
		<p class="mt-1.5 text-[12.5px] text-[var(--mf-text-4)]">
			Las canciones que descargues aparecerán aquí.
		</p>
	</div>
</section>
