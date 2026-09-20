<script lang="ts">
	import Cover from './Cover.svelte';

	interface Props {
		playlistId?: string;
		coverUrl?: string;
		trackIds: (string | number)[];
		size?: 'small' | 'medium' | 'large';
		version?: string;
		class?: string;
	}

	let {
		playlistId,
		coverUrl,
		trackIds,
		size = 'medium',
		version,
		class: klass = ''
	}: Props = $props();

	let coverFailed = $state(false);

	const mosaic = $derived(trackIds.length >= 4);
	const single = $derived(trackIds.length >= 1 && trackIds.length < 4 ? trackIds[0] : null);
	const src = $derived(
		coverUrl
			? coverUrl
			: playlistId
				? `/api/playlists/${playlistId}/cover?size=${size}${version ? `&v=${encodeURIComponent(version)}` : ''}`
				: undefined
	);
</script>

{#if src && !coverFailed}
	<div class="relative overflow-hidden bg-surface {klass}">
		<img
			{src}
			alt=""
			loading="lazy"
			onerror={() => (coverFailed = true)}
			class="h-full w-full object-cover"
		/>
	</div>
{:else if mosaic}
	<div class="grid grid-cols-2 grid-rows-2 overflow-hidden {klass}">
		{#each trackIds.slice(0, 4) as id (id)}
			<Cover trackId={id} size="small" class="h-full w-full" />
		{/each}
	</div>
{:else if single !== null}
	<Cover trackId={single} size="medium" class={klass} />
{:else}
	<div class="flex items-center justify-center bg-surface {klass}">
		<svg viewBox="0 0 24 24" fill="currentColor" class="h-2/5 w-2/5 text-fg-3">
			<path
				d="M19.952 1.651a.75.75 0 0 1 .298.599V16.303a3 3 0 0 1-2.176 2.884l-1.32.377a2.553 2.553 0 1 1-1.401-4.909l2.311-.66a1.5 1.5 0 0 0 1.088-1.442V6.994l-9 2.572v9.737a3 3 0 0 1-2.176 2.884l-1.32.377a2.553 2.553 0 1 1-1.402-4.909l2.312-.66a1.5 1.5 0 0 0 1.088-1.442V5.25a.75.75 0 0 1 .544-.721l10.5-3a.75.75 0 0 1 .754.122Z"
			/>
		</svg>
	</div>
{/if}
