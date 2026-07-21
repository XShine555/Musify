<script lang="ts">
	import { enhance } from '$app/forms';
	import Headphones from '@lucide/svelte/icons/headphones';
	import Music from '@lucide/svelte/icons/music';
	import ImageIcon from '@lucide/svelte/icons/image';
	import Check from '@lucide/svelte/icons/check';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import Surface from '$lib/components/ui/Surface.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Field from '$lib/components/ui/Field.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Checkbox from '$lib/components/ui/Checkbox.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	type Status = 'idle' | 'uploading' | 'done' | 'error';
	type StepState = 'idle' | 'active' | 'done';

	const MAX_TITLE = 100;

	let title = $state('');
	let audioName = $state('');
	let audioSize = $state(0);
	let coverPreview = $state('');
	let dragging = $state(false);
	let status = $state<Status>('idle');
	let acceptedTerms = $state(false);
	let errorMsg = $state('');
	let publishedTitle = $state('');
	let audioInput = $state<HTMLInputElement>();

	const steps = [
		{ key: 'upload', label: 'Subir', hint: 'Portada y audio al almacenamiento' },
		{ key: 'process', label: 'Procesar', hint: 'Transcodificado para streaming' },
		{ key: 'publish', label: 'Publicar', hint: 'Disponible en el catálogo' }
	];

	const busy = $derived(status === 'uploading');

	const canPublish = $derived(
		title.trim() !== '' && audioName !== '' && coverPreview !== '' && acceptedTerms && !busy
	);

	const stepStates = $derived.by<StepState[]>(() => {
		if (status === 'done') return ['done', 'done', 'done'];
		if (status === 'uploading') return ['active', 'idle', 'idle'];
		return ['idle', 'idle', 'idle'];
	});

	const dropClass = $derived(
		dragging || audioName !== '' ? 'border-accent/60' : 'border-line hover:border-accent/60'
	);

	function stepClass(s: StepState) {
		if (s === 'active') return 'border-accent/40 bg-accent/5';
		if (s === 'done') return 'border-accent/40';
		return '';
	}

	function formatSize(bytes: number) {
		if (bytes === 0) return '';
		const mb = bytes / (1024 * 1024);
		return mb >= 1 ? `${mb.toFixed(1)} MB` : `${Math.round(bytes / 1024)} KB`;
	}

	function pickAudio(file: File) {
		audioName = file.name;
		audioSize = file.size;
		if (title.trim() === '') title = file.name.replace(/\.[^.]+$/, '');
	}

	function onAudioInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (file) pickAudio(file);
	}

	function onDrop(event: DragEvent) {
		event.preventDefault();
		dragging = false;
		const file = event.dataTransfer?.files?.[0];
		if (!file || !audioInput) return;
		const dt = new DataTransfer();
		dt.items.add(file);
		audioInput.files = dt.files;
		pickAudio(file);
	}

	function onCoverInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (file) coverPreview = URL.createObjectURL(file);
	}

	function reset() {
		title = '';
		audioName = '';
		audioSize = 0;
		coverPreview = '';
		status = 'idle';
		acceptedTerms = false;
		errorMsg = '';
	}
</script>

<svelte:head>
	<title>Subir música · Musify</title>
	<meta name="description" content="Sube tus canciones a Musify: portada, audio y título." />
</svelte:head>

<Page>
	<PageHeader
		title="Subir música"
		subtitle="Añade una canción con su portada y título. Nosotros la procesamos para streaming."
	/>

	<ol class="mt-8 grid gap-3 sm:mt-10 sm:grid-cols-3 sm:gap-4">
		{#each steps as step, i (step.key)}
			<li>
				<Surface class={stepClass(stepStates[i])}>
					<div class="flex items-center gap-3">
						<span
							class="grid h-8 w-8 place-items-center rounded-full bg-accent-soft text-sm font-bold text-on-accent"
						>
							{#if stepStates[i] === 'done'}
								<Check class="h-4 w-4" strokeWidth={2.5} />
							{:else}
								{i + 1}
							{/if}
						</span>
						<span class="text-fg">{step.label}</span>
					</div>
					<p class="mt-3 text-base text-muted">{step.hint}</p>
				</Surface>
			</li>
		{/each}
	</ol>

	{#if status === 'done'}
		<Surface padding="lg" class="mt-10 border-accent/30 bg-accent/10 text-center">
			<span
				class="mx-auto grid h-16 w-16 place-items-center rounded-full bg-accent-soft text-on-accent"
			>
				<Check class="h-8 w-8" strokeWidth={2.5} />
			</span>
			<h2 class="mt-6 text-2xl font-semibold tracking-tight text-fg">¡Subida!</h2>
			<p class="mt-3 text-fg-2">
				«{publishedTitle}» se está procesando. Aparecerá lista para reproducir en tu biblioteca en
				unos momentos.
			</p>
			<div class="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
				<Button href="/library" size="lg" class="w-full sm:w-auto">Ir a la biblioteca</Button>
				<Button variant="secondary" size="lg" onclick={reset} class="w-full sm:w-auto">
					Subir otra
				</Button>
			</div>
		</Surface>
	{:else}
		<form
			method="POST"
			enctype="multipart/form-data"
			use:enhance={() => {
				status = 'uploading';
				errorMsg = '';
				return async ({ result }) => {
					if (result.type === 'success') {
						publishedTitle = title;
						status = 'done';
					} else if (result.type === 'failure') {
						status = 'error';
						errorMsg = (result.data?.message as string) ?? 'No se pudo subir la canción.';
					} else {
						status = 'error';
						errorMsg = 'Error inesperado durante la subida.';
					}
				};
			}}
			class="mt-10 space-y-8"
		>
			<div
				role="button"
				tabindex="0"
				ondragover={(e) => {
					e.preventDefault();
					dragging = true;
				}}
				ondragleave={() => (dragging = false)}
				ondrop={onDrop}
				class="relative flex min-h-56 flex-col items-center justify-center rounded-panel border border-dashed bg-surface p-6 text-center transition sm:min-h-72 sm:p-10 {dropClass}"
			>
				<input
					bind:this={audioInput}
					type="file"
					name="audio"
					accept="audio/*"
					onchange={onAudioInput}
					class="absolute inset-0 cursor-pointer opacity-0"
					aria-label="Seleccionar archivo de audio"
				/>
				<span
					class="grid h-16 w-16 place-items-center rounded-full bg-accent-soft text-on-accent sm:h-20 sm:w-20"
				>
					{#if audioName === ''}
						<Headphones class="h-8 w-8 sm:h-9 sm:w-9" />
					{:else}
						<Music class="h-8 w-8 sm:h-9 sm:w-9" />
					{/if}
				</span>
				{#if audioName === ''}
					<p class="mt-5 text-fg sm:text-lg">Arrastra tu audio aquí o haz clic para elegir</p>
					<p class="mt-2 text-sm text-muted">MP3, FLAC, WAV…</p>
				{:else}
					<p class="mt-5 max-w-full truncate text-fg sm:text-lg">
						{audioName}
					</p>
					<p class="mt-2 text-sm text-muted">{formatSize(audioSize)} · Listo para subir</p>
				{/if}
			</div>

			<div class="grid items-start gap-6 sm:grid-cols-[auto_1fr] sm:gap-8">
				<Field label="Portada">
					<label
						class="relative grid h-36 w-36 cursor-pointer place-items-center overflow-hidden rounded-control border border-line bg-surface transition hover:border-accent/50 sm:h-44 sm:w-44"
					>
						<input
							type="file"
							name="cover"
							accept="image/*"
							onchange={onCoverInput}
							class="absolute inset-0 cursor-pointer opacity-0"
							aria-label="Seleccionar portada"
						/>
						{#if coverPreview !== ''}
							<img src={coverPreview} alt="Portada" class="h-full w-full object-cover" />
						{:else}
							<ImageIcon class="h-12 w-12 text-muted" />
						{/if}
					</label>
				</Field>

				<Field label="Título" for="title">
					<Input
						id="title"
						name="title"
						bind:value={title}
						maxlength={MAX_TITLE}
						placeholder="Nombre de la canción"
					/>
					{#snippet hint()}
						<p>Se usa como nombre público de la pista en el catálogo.</p>
						<span class="shrink-0 tabular-nums">{title.length}/{MAX_TITLE}</span>
					{/snippet}
				</Field>
			</div>

			<Checkbox bind:checked={acceptedTerms}>
				Acepto los
				<a
					href="/terms"
					class="text-accent-soft underline underline-offset-2 transition hover:brightness-110"
				>
					términos y condiciones
				</a>
				al subir {title.trim() !== '' ? `«${title}»` : 'esta canción'}.
			</Checkbox>

			{#if status === 'uploading'}
				<Surface>
					<div class="flex items-center justify-between text-base">
						<span class="font-medium text-fg">Subiendo y creando la pista…</span>
						<span class="text-muted">un momento</span>
					</div>
					<div class="mt-4 h-2.5 overflow-hidden rounded-full bg-surface-2">
						<div class="h-full w-full animate-pulse rounded-full bg-accent-soft"></div>
					</div>
				</Surface>
			{/if}

			{#if status === 'error'}
				<Alert tone="danger">{errorMsg}</Alert>
			{/if}

			<div class="flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-end sm:gap-4">
				<Button href="/explore" variant="secondary" size="lg" class="w-full sm:w-auto">
					Cancelar
				</Button>
				<Button type="submit" size="lg" disabled={!canPublish} class="w-full sm:w-auto">
					Publicar
				</Button>
			</div>
		</form>
	{/if}
</Page>
