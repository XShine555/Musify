<script lang="ts">
	import UploadCloud from '@lucide/svelte/icons/cloud-upload';
	import { genreInfo } from '$lib/data/genres';
	import {
		filesFromDrop,
		readDuration,
		summarizeQueue,
		toQueuedUpload,
		UPLOAD_FORMATS,
		type QueuedUpload,
		type Visibility
	} from '$lib/data/uploadQueue';
	import { plural } from '$lib/utils/format';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import UploadQueueList from '$lib/components/music/library/UploadQueueList.svelte';
	import UploadSettings from '$lib/components/music/library/UploadSettings.svelte';

	interface Props {
		genres: { genre: string; incompatibleWith: string[] }[];
	}

	let { genres }: Props = $props();

	const ACCEPT = UPLOAD_FORMATS.map((ext) => `.${ext}`).join(',');

	let queue = $state<QueuedUpload[]>([]);
	let selectedGenres = $state<string[]>([]);
	let visibility = $state<Visibility>('private');
	let coauthors = $state<string[]>([]);
	let dragging = $state(false);
	let input = $state<HTMLInputElement>();

	const single = $derived(queue.length === 1 ? queue[0] : null);
	const summary = $derived(summarizeQueue(queue));
	const genresLabel = $derived(
		selectedGenres.length > 0
			? selectedGenres.map((genre) => genreInfo(genre).label).join(', ')
			: '-'
	);
	const visibilityLabel = $derived(visibility === 'private' ? 'Privado' : 'Público');
	const queueMeta = $derived(
		[
			plural(summary.ready, 'lista', 'listas'),
			summary.failed > 0 ? `${summary.failed} con error` : null
		]
			.filter(Boolean)
			.join(' · ')
	);

	function enqueue(files: File[]) {
		const added = files.map(toQueuedUpload);
		queue = [...queue, ...added];
		for (const item of added) {
			if (item.error) continue;
			readDuration(item.file).then((duration) => {
				queue = queue.map((current) =>
					current.id === item.id ? { ...current, duration } : current
				);
			});
		}
	}

	function onPick(event: Event) {
		const target = event.currentTarget as HTMLInputElement;
		enqueue(Array.from(target.files ?? []));
		target.value = '';
	}

	async function onDrop(event: DragEvent) {
		event.preventDefault();
		dragging = false;
		if (!event.dataTransfer) return;
		enqueue(await filesFromDrop(event.dataTransfer));
	}

	function setTitle(id: string, title: string) {
		queue = queue.map((item) => (item.id === id ? { ...item, title } : item));
	}

	function remove(id: string) {
		queue = queue.filter((item) => item.id !== id);
	}
</script>

<div class="mt-6 grid items-start gap-6 lg:grid-cols-2">
	<div class="flex min-w-0 flex-col gap-8">
		<div
			role="presentation"
			ondragover={(event) => {
				event.preventDefault();
				dragging = true;
			}}
			ondragleave={() => (dragging = false)}
			ondrop={onDrop}
			class="flex min-h-64 flex-col items-center justify-center rounded-panel border border-dashed p-6 text-center transition sm:p-10 {dragging
				? 'border-accent/60 bg-accent-tint'
				: 'border-line-strong bg-surface'}"
		>
			<span class="grid size-14 place-items-center rounded-full bg-surface-2 text-fg-2">
				<UploadCloud class="size-icon-xl" />
			</span>
			<h2 class="mt-5 font-display text-xl font-semibold tracking-display text-fg">
				Arrastra tus archivos aquí
			</h2>
			<p class="mt-2 text-xs text-fg-2">
				WAV, FLAC, AIFF o MP3 · hasta 500 MB por archivo · puedes soltar carpetas enteras
			</p>
			<Button class="mt-6" onclick={() => input?.click()}>Elegir archivos</Button>
			<input
				bind:this={input}
				type="file"
				multiple
				accept={ACCEPT}
				onchange={onPick}
				class="hidden"
				aria-label="Elegir archivos de audio"
				tabindex="-1"
			/>
		</div>

		<UploadSettings
			{genres}
			bind:selectedGenres
			bind:visibility
			bind:coauthors
			bind:title={() => single?.title ?? '', (value) => single && setTitle(single.id, value)}
			titleDisabled={single === null}
		/>
	</div>

	<section class="min-w-0 rounded-panel bg-surface p-5 sm:p-6">
		<div class="flex flex-wrap items-start justify-between gap-4">
			<div>
				<h2 class="font-display text-xl font-semibold tracking-display text-fg">Cola de subida</h2>
				<p class="mt-1 text-xs text-fg-2">
					{queue.length > 0 ? queueMeta : 'Todavía no hay archivos'}
				</p>
			</div>
			<div class="flex items-center gap-3">
				{#if queue.length > 0}
					<Button variant="secondary" size="sm" onclick={() => (queue = [])}>Vaciar cola</Button>
				{/if}
				<Button disabled>Añadir {summary.ready} a la biblioteca</Button>
			</div>
		</div>
		{#if queue.length > 0}
			<UploadQueueList
				class="mt-4"
				items={queue}
				{genresLabel}
				{visibilityLabel}
				onRemove={remove}
				onTitle={setTitle}
			/>
		{:else}
			<p class="mt-8 mb-6 text-center text-sm text-fg-2">
				Los archivos que añadas aparecerán aquí.
			</p>
		{/if}
	</section>
</div>
