<script lang="ts">
	import { page } from '$app/state';
	import Button from '$lib/components/ui/primitives/Button.svelte';
</script>

<div class="animate-enter mx-auto my-[14vh] max-w-prose-sm text-center">
	<h1 class="mb-2.5 text-display-3 text-fg">
		{page.status === 404 ? 'Página no encontrada' : 'No se pudo cargar tu música'}
	</h1>
	<p class="mb-6.5 text-body text-fg-2">
		{page.status === 404
			? 'Comprueba el enlace o vuelve al inicio.'
			: 'La conexión se interrumpió. Lo que tengas descargado seguirá disponible sin conexión.'}
	</p>
	<Button href="/" variant="secondary">Volver</Button>
</div>
