<script lang="ts">
	import { page } from '$app/state';
	import Button from '$lib/components/ui/primitives/Button.svelte';
</script>

<div class="min-h-screen bg-bg px-6 text-fg antialiased">
	<div class="animate-enter mx-auto my-[14vh] max-w-prose-sm text-center">
		<h1 class="mb-2.5 text-display-3">
			{page.status === 404 ? 'Página no encontrada' : 'Algo salió mal'}
		</h1>
		<p class="mb-6.5 text-body text-fg-2">
			{page.status === 404
				? 'Comprueba el enlace o vuelve al inicio.'
				: 'No se pudo cargar la página. Inténtalo de nuevo en unos momentos.'}
		</p>
		<Button href="/" variant="secondary">Volver al inicio</Button>
	</div>
</div>
