<script lang="ts">
	import { page } from '$app/state';
</script>

<div class="min-h-screen bg-bg text-fg antialiased">
	<div class="mx-auto my-[14vh] max-w-prose-sm text-center">
		<h1 class="mb-2.5 text-display-3">
			{page.status === 404 ? 'Página no encontrada' : 'Algo salió mal'}
		</h1>
		<a href="/" class="text-body text-fg-2 underline">Volver al inicio</a>
	</div>
</div>
