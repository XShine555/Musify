<script lang="ts">
	import { enhance } from '$app/forms';
	import Headphones from '@lucide/svelte/icons/headphones';
	import Music from '@lucide/svelte/icons/music';
	import ImageIcon from '@lucide/svelte/icons/image';
	import Check from '@lucide/svelte/icons/check';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Surface from '$lib/components/ui/primitives/Surface.svelte';
	import ImageDropzone from '$lib/components/ui/forms/ImageDropzone.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Field from '$lib/components/ui/primitives/Field.svelte';
	import Input from '$lib/components/ui/primitives/Input.svelte';
	import Checkbox from '$lib/components/ui/primitives/Checkbox.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import Chip from '$lib/components/ui/primitives/Chip.svelte';
	import { genreOptions } from '$lib/data/genres';

	type Status = 'idle' | 'uploading' | 'done' | 'error';
	type StepState = 'idle' | 'active' | 'done';

	const MAX_TITLE = 100;

	let { data } = $props();

	let title = $state('');
	let audioName = $state('');
	let audioSize = $state(0);
	let hasCover = $state(false);
	let coverResetToken = $state(0);
	let dragging = $state(false);
	let status = $state<Status>('idle');
	let acceptedTerms = $state(false);
	let errorMsg = $state('');
	let errorDetail = $state('');
	let tags = $state<string[]>([]);
	let publishedTitle = $state('');
	let audioInput = $state<HTMLInputElement>();

	const steps = [
		{ key: 'upload', label: 'Subir', hint: 'Portada y audio.' },
		{ key: 'process', label: 'Procesar', hint: 'Transcodificado para streaming.' },
		{ key: 'publish', label: 'Publicar', hint: 'Disponible en el catálogo.' }
	];

	const options = $derived(genreOptions(data.genres));
	const blocked = $derived(
		new Set(
			options
				.filter((option) => tags.includes(option.genre))
				.flatMap((option) => option.incompatibleWith)
		)
	);

	const busy = $derived(status === 'uploading');

	const canPublish = $derived(
		title.trim() !== '' && audioName !== '' && hasCover && tags.length > 0 && acceptedTerms && !busy
	);

	const stepStates = $derived.by<StepState[]>(() => {
		if (status === 'done') return ['done', 'done', 'done'];
		if (status === 'uploading') return ['active', 'idle', 'idle'];
		return ['idle', 'idle', 'idle'];
	});

	function stepClass(s: StepState) {
		return s === 'idle' ? '' : 'bg-accent-tint';
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

	function toggleTag(tag: string) {
		tags = tags.includes(tag) ? tags.filter((current) => current !== tag) : [...tags, tag];
	}

	function reset() {
		title = '';
		audioName = '';
		audioSize = 0;
		hasCover = false;
		coverResetToken += 1;
		status = 'idle';
		acceptedTerms = false;
		errorMsg = '';
		errorDetail = '';
		tags = [];
	}
</script>

<svelte:head>
	<title>Subir música</title>
	<meta name="description" content="Sube tus canciones a Musify: portada, audio y título." />
</svelte:head>

<Page>
	<PageHeader
		title="Subir música"
		description="Añade una canción con su portada y título. Nosotros la procesamos para streaming."
	/>

	<ol class="grid gap-3 sm:grid-cols-3 sm:gap-4">
		{#each steps as step, i (step.key)}
			<li>
				<Surface padding="sm" class={stepClass(stepStates[i])}>
					<div class="flex items-center gap-3">
						<span
							class="grid size-7 shrink-0 place-items-center rounded-full text-sm {stepStates[i] ===
							'idle'
								? 'bg-surface-2 text-fg-2'
								: 'bg-accent-btn text-accent-soft'}"
						>
							{#if stepStates[i] === 'done'}
								<Check class="size-3.5" strokeWidth={2.5} />
							{:else}
								{i + 1}
							{/if}
						</span>
						<span class="text-sm font-medium text-fg">{step.label}</span>
					</div>
					<p class="mt-2.5 text-xs text-fg-2">{step.hint}</p>
				</Surface>
			</li>
		{/each}
	</ol>

	{#if status === 'done'}
		<Surface padding="lg" class="mt-10 bg-accent-tint text-center">
			<span class="mx-auto grid size-14 place-items-center rounded-full bg-accent-soft text-ink">
				<Check class="size-7" strokeWidth={2.5} />
			</span>
			<h2 class="mt-6 font-display text-2xl font-semibold tracking-display text-fg">¡Subida!</h2>
			<p class="mt-3 text-sm text-fg-2">
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
				errorDetail = '';
				return async ({ result }) => {
					if (result.type === 'success') {
						publishedTitle = title;
						status = 'done';
					} else if (result.type === 'failure') {
						status = 'error';
						errorMsg = (result.data?.message as string) ?? 'No se pudo subir la canción.';
						errorDetail = (result.data?.detail as string | undefined) ?? '';
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
				class="relative flex min-h-48 flex-col items-center justify-center rounded-panel p-6 text-center transition sm:min-h-64 sm:p-10 {dragging
					? 'bg-surface-2 ring-2 ring-accent/50'
					: 'bg-surface'}"
			>
				<input
					bind:this={audioInput}
					type="file"
					name="audio"
					accept="audio/*"
					onchange={onAudioInput}
					class="absolute inset-0 cursor-pointer opacity-0 focus:outline-none"
					aria-label="Seleccionar archivo de audio"
				/>
				{#if audioName === ''}
					<Headphones class="size-9 text-fg-2 sm:size-10" strokeWidth={1.4} />
				{:else}
					<Music class="size-9 text-accent-soft sm:size-10" strokeWidth={1.4} />
				{/if}
				{#if audioName === ''}
					<p class="mt-4 text-sm font-medium text-fg">
						Arrastra tu audio aquí o haz clic para elegir
					</p>
					<p class="mt-1.5 text-xs text-fg-2">MP3, FLAC, WAV…</p>
				{:else}
					<p class="mt-4 max-w-full truncate text-sm font-medium text-fg">
						{audioName}
					</p>
					<p class="mt-1.5 text-xs text-fg-2">{formatSize(audioSize)} · Listo para subir</p>
				{/if}
			</div>

			<div class="grid items-start gap-6 sm:grid-cols-[auto_1fr] sm:gap-8">
				<Field label="Portada">
					{#key coverResetToken}
						<ImageDropzone
							name="cover"
							icon={ImageIcon}
							gradient
							size="hero"
							onselect={() => (hasCover = true)}
						/>
					{/key}
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

			<Field label="Géneros">
				<div class="flex flex-wrap gap-2">
					{#each options as option (option.genre)}
						<Chip
							selected={tags.includes(option.genre)}
							disabled={blocked.has(option.genre)}
							onclick={() => toggleTag(option.genre)}
						>
							{option.label}
						</Chip>
					{/each}
				</div>
				{#each tags as tag (tag)}
					<input type="hidden" name="tags" value={tag} />
				{/each}
				{#snippet hint()}
					<p>Elige al menos uno. Algunos géneros no se pueden combinar, como Metal con Ambient.</p>
				{/snippet}
			</Field>

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
				<Surface padding="sm">
					<div class="flex items-center justify-between text-sm">
						<span class="font-medium text-fg">Subiendo y creando la pista…</span>
						<span class="text-xs text-fg-2">un momento</span>
					</div>
					<div class="mt-3 h-2 overflow-hidden rounded-full bg-surface-2">
						<div class="h-full w-full animate-pulse rounded-full bg-accent-soft"></div>
					</div>
				</Surface>
			{/if}

			{#if status === 'error'}
				<Alert tone="danger">
					{errorMsg}
					{#if errorDetail}<span class="mt-1 block text-xs opacity-80">{errorDetail}</span>{/if}
				</Alert>
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
