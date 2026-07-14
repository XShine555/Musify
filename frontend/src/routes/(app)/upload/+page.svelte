<script lang="ts">
	import { enhance } from '$app/forms';
	import Headphones from '@lucide/svelte/icons/headphones';
	import Music from '@lucide/svelte/icons/music';
	import ImageIcon from '@lucide/svelte/icons/image';
	import Check from '@lucide/svelte/icons/check';
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';

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
		dragging || audioName !== ''
			? 'border-emerald-500/60'
			: 'border-white/15 hover:border-emerald-500/60'
	);

	function stepClass(s: StepState) {
		if (s === 'active') return 'border-emerald-500/40 bg-emerald-500/5';
		if (s === 'done') return 'border-emerald-500/40';
		return 'border-white/15';
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

<section class="px-8 pt-8 pb-16 animate-[fade-up_0.45s_cubic-bezier(0.16,1,0.3,1)]">
	<a
		href="/library"
		class="group inline-flex items-center gap-2 text-sm font-medium text-neutral-400 transition-colors hover:text-white"
	>
		<span
			class="grid h-8 w-8 place-items-center rounded-lg border border-white/15 transition-all duration-200 group-hover:-translate-x-0.5 group-hover:border-emerald-500/60 group-hover:text-emerald-400"
		>
			<ArrowLeft class="h-4 w-4" />
		</span>
		Volver a la biblioteca
	</a>
	<h1 class="mt-6 text-4xl font-bold tracking-tight sm:text-5xl">Subir música</h1>
	<p class="mt-4 text-lg text-neutral-400">
		Añade una canción con su portada y título. Nosotros la procesamos para streaming.
	</p>

	<ol class="mt-10 grid gap-4 sm:grid-cols-3">
		{#each steps as step, i (step.key)}
			<li class="rounded-xl border p-5 transition {stepClass(stepStates[i])}">
				<div class="flex items-center gap-3">
					<span
						class="grid h-8 w-8 place-items-center rounded-full bg-[var(--mf-accent)] text-sm font-bold text-neutral-950"
					>
						{#if stepStates[i] === 'done'}
							<Check class="h-4 w-4" strokeWidth={2.5} />
						{:else}
							{i + 1}
						{/if}
					</span>
					<span class="text-base font-semibold">{step.label}</span>
				</div>
				<p class="mt-3 text-sm text-neutral-500">{step.hint}</p>
			</li>
		{/each}
	</ol>

	{#if status === 'done'}
		<div class="mt-10 rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-12 text-center">
			<span
				class="mx-auto grid h-16 w-16 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950"
			>
				<Check class="h-8 w-8" strokeWidth={2.5} />
			</span>
			<h2 class="mt-6 text-2xl font-bold tracking-tight">¡Subida!</h2>
			<p class="mt-3 text-lg text-neutral-300">
				«{publishedTitle}» se está procesando. Aparecerá lista para reproducir en tu biblioteca en
				unos momentos.
			</p>
			<div class="mt-8 flex items-center justify-center gap-4">
				<a
					href="/library"
					class="rounded-lg bg-[var(--mf-accent)] px-8 py-3.5 text-base font-semibold text-neutral-950 transition hover:brightness-110"
				>
					Ir a la biblioteca
				</a>
				<button
					type="button"
					onclick={reset}
					class="rounded-lg border border-white/20 px-8 py-3.5 text-base font-semibold text-white backdrop-blur-md transition hover:bg-white/5"
				>
					Subir otra
				</button>
			</div>
		</div>
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
				class="relative flex min-h-72 flex-col items-center justify-center rounded-xl border border-dashed bg-neutral-950 p-10 text-center transition {dropClass}"
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
				<span class="grid h-20 w-20 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950">
					{#if audioName === ''}
						<Headphones class="h-9 w-9" />
					{:else}
						<Music class="h-9 w-9" />
					{/if}
				</span>
				{#if audioName === ''}
					<p class="mt-5 text-lg font-medium text-neutral-200">
						Arrastra tu audio aquí o haz clic para elegir
					</p>
					<p class="mt-2 text-sm text-neutral-500">MP3, FLAC, WAV…</p>
				{:else}
					<p class="mt-5 max-w-full truncate text-lg font-medium text-neutral-100">{audioName}</p>
					<p class="mt-2 text-sm text-neutral-500">{formatSize(audioSize)} · Listo para subir</p>
				{/if}
			</div>

			<div class="grid items-start gap-8 sm:grid-cols-[auto_1fr]">
				<div>
					<span class="mb-3 block text-sm font-medium text-neutral-300">Portada</span>
					<label
						class="relative grid h-44 w-44 cursor-pointer place-items-center overflow-hidden rounded-xl border border-white/15 bg-neutral-950 transition hover:border-emerald-500/40"
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
							<ImageIcon class="h-12 w-12 text-neutral-500" />
						{/if}
					</label>
				</div>

				<div class="flex flex-col">
					<label for="title" class="mb-3 block text-sm font-medium text-neutral-300">Título</label>
					<input
						id="title"
						name="title"
						type="text"
						bind:value={title}
						maxlength={MAX_TITLE}
						placeholder="Nombre de la canción"
						class="w-full rounded-lg border border-white/15 bg-neutral-950 px-5 py-4 text-base text-neutral-100 placeholder:text-neutral-500 focus:border-emerald-500/60 focus:outline-none"
					/>
					<div class="mt-3 flex items-center justify-between gap-3 text-sm text-neutral-500">
						<p>Se usa como nombre público de la pista en el catálogo.</p>
						<span class="shrink-0 tabular-nums">{title.length}/{MAX_TITLE}</span>
					</div>
				</div>
			</div>

			<label class="flex items-center gap-3 text-sm text-neutral-300">
				<span class="relative inline-grid h-5 w-5 shrink-0 place-items-center">
					<input
						type="checkbox"
						bind:checked={acceptedTerms}
						class="peer h-5 w-5 cursor-pointer appearance-none rounded border border-white/15 bg-neutral-950 transition checked:border-emerald-500 checked:bg-[var(--mf-accent)] focus-visible:border-emerald-500/60 focus-visible:outline-none"
					/>
					<Check
						class="pointer-events-none absolute h-3.5 w-3.5 text-neutral-950 opacity-0 transition peer-checked:opacity-100"
						strokeWidth={3}
					/>
				</span>
				<span>
					Acepto los
					<a
						href="/terms"
						class="text-emerald-400 underline underline-offset-2 transition hover:text-emerald-300"
					>
						términos y condiciones
					</a>
					al subir {title.trim() !== '' ? `«${title}»` : 'esta canción'}.
				</span>
			</label>

			{#if status === 'uploading'}
				<div class="rounded-xl border border-white/15 bg-neutral-950 p-6">
					<div class="flex items-center justify-between text-base">
						<span class="font-medium text-neutral-200">Subiendo y creando la pista…</span>
						<span class="text-neutral-500">un momento</span>
					</div>
					<div class="mt-4 h-2.5 overflow-hidden rounded-full bg-white/10">
						<div class="h-full w-full animate-pulse rounded-full bg-[var(--mf-accent)]"></div>
					</div>
				</div>
			{/if}

			{#if status === 'error'}
				<div class="rounded-xl border border-red-500/40 bg-red-500/10 p-5 text-sm text-red-200">
					{errorMsg}
				</div>
			{/if}

			<div class="flex items-center justify-end gap-4">
				<a
					href="/explore"
					class="rounded-lg border border-white/20 px-8 py-3.5 text-base font-semibold text-white backdrop-blur-md transition hover:bg-white/5"
				>
					Cancelar
				</a>
				<button
					type="submit"
					disabled={!canPublish}
					class="cursor-pointer rounded-lg bg-[var(--mf-accent)] px-8 py-3.5 text-base font-semibold text-neutral-950 backdrop-blur-md transition hover:brightness-110 hover:shadow-[0_10px_28px_-8px] active:scale-[0.97] disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:shadow-none"
				>
					Publicar
				</button>
			</div>
		</form>
	{/if}
</section>
