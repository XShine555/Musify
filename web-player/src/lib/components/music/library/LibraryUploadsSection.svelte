<script lang="ts">
	import { genreLabels } from '$lib/data/genres';
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
	import type { GenreOption } from '$lib/types';
	import FileDropzone from '$lib/components/ui/forms/FileDropzone.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import UploadQueueList from '$lib/components/music/library/UploadQueueList.svelte';
	import UploadSettings from '$lib/components/music/library/UploadSettings.svelte';

	interface Props {
		genres: GenreOption[];
	}

	let { genres }: Props = $props();

	const ACCEPT = UPLOAD_FORMATS.map((ext) => `.${ext}`).join(',');

	let queue = $state<QueuedUpload[]>([]);
	let selectedGenres = $state<string[]>([]);
	let visibility = $state<Visibility>('private');
	let coauthors = $state<string[]>([]);

	const single = $derived(queue.length === 1 ? queue[0] : null);
	const summary = $derived(summarizeQueue(queue));
	const genresLabel = $derived(genreLabels(selectedGenres));
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

	function setTitle(id: string, title: string) {
		queue = queue.map((item) => (item.id === id ? { ...item, title } : item));
	}

	function remove(id: string) {
		queue = queue.filter((item) => item.id !== id);
	}
</script>

<div class="mt-6 grid items-start gap-6 lg:grid-cols-2">
	<div class="flex min-w-0 flex-col gap-8">
		<FileDropzone
			accept={ACCEPT}
			title="Arrastra tus archivos aquí"
			hint="WAV, FLAC, AIFF o MP3 · hasta 500 MB por archivo · puedes soltar carpetas enteras"
			inputLabel="Elegir archivos de audio"
			onfiles={enqueue}
			readDrop={filesFromDrop}
		/>

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
