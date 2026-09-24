<script lang="ts">
	import { genreLabels } from '$lib/data/genres';
	import { filesFromDrop, UPLOAD_FORMATS, uploadLimitsLabel } from '$lib/data/uploadQueue';
	import { uploadQueue as queue } from '$lib/state/uploadQueue.svelte';
	import { plural } from '$lib/utils/format';
	import type { GenreOption } from '$lib/types';
	import FileDropzone from '$lib/components/ui/forms/FileDropzone.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import UploadQueueList from '$lib/components/music/library/UploadQueueList.svelte';
	import UploadSettings from '$lib/components/music/library/UploadSettings.svelte';

	interface AlbumChoice {
		id: string;
		title: string;
	}

	interface Props {
		genres: GenreOption[];
		albums: AlbumChoice[];
	}

	let { genres, albums }: Props = $props();

	const ACCEPT = UPLOAD_FORMATS.map((ext) => `.${ext}`).join(',');

	const summary = $derived(queue.summary);
	const genresLabel = $derived(genreLabels(queue.selectedGenres));
	const queueMeta = $derived(
		[
			summary.ready > 0 ? plural(summary.ready, 'lista', 'listas') : null,
			summary.uploading > 0 ? `${summary.uploading} subiendo` : null,
			summary.processing > 0 ? `${summary.processing} procesando` : null,
			summary.done > 0 ? plural(summary.done, 'terminada', 'terminadas') : null,
			summary.errored + summary.failed > 0 ? `${summary.errored + summary.failed} con error` : null
		]
			.filter(Boolean)
			.join(' · ')
	);
</script>

<div class="mt-6 grid items-start gap-6 lg:grid-cols-2">
	<div class="flex min-w-0 flex-col gap-8">
		<FileDropzone
			accept={ACCEPT}
			title="Arrastra tus archivos aquí"
			hint={uploadLimitsLabel()}
			inputLabel="Elegir archivos de audio"
			onfiles={(files) => queue.enqueue(files)}
			readDrop={filesFromDrop}
		/>

		<UploadSettings
			{genres}
			{albums}
			bind:selectedGenres={queue.selectedGenres}
			bind:albumId={queue.albumId}
			hasCover={queue.cover !== null}
			oncover={(file) => (queue.cover = file)}
			disabled={queue.running}
		/>
	</div>

	<section class="min-w-0 rounded-panel bg-surface p-5 sm:p-6">
		<div class="flex flex-wrap items-start justify-between gap-4">
			<div>
				<h2 class="text-display-3 text-fg">Cola de subida</h2>
				<p class="mt-1 text-xs text-fg-2">
					{queue.items.length > 0 ? queueMeta : 'Todavía no hay archivos'}
				</p>
			</div>
			<div class="flex items-center gap-3">
				{#if queue.items.length > 0}
					<Button variant="secondary" size="sm" onclick={() => queue.clear()}>Vaciar cola</Button>
				{/if}
				<Button
					disabled={summary.ready === 0}
					loading={queue.running}
					onclick={() => queue.start(genres)}
				>
					Subir {summary.ready}
				</Button>
			</div>
		</div>

		<p class="mt-3 text-xs text-fg-2">
			Se añadirá sin publicar: publícala desde Canciones cuando termine de procesarse.
		</p>

		{#if queue.notice}
			<Alert tone="danger" class="mt-4">{queue.notice}</Alert>
		{/if}

		{#if queue.items.length > 0}
			<UploadQueueList
				class="mt-4"
				items={queue.items}
				{genresLabel}
				onRemove={(id) => queue.remove(id)}
				onCancel={(id) => queue.cancel(id)}
				onRetry={(id) => queue.retry(id)}
				onTitle={(id, title) => queue.setTitle(id, title)}
			/>
		{:else}
			<p class="mt-8 mb-6 text-center text-sm text-fg-2">
				Los archivos que añadas aparecerán aquí.
			</p>
		{/if}
	</section>
</div>
