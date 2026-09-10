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
	<div class="bg-surface {klass}"></div>
{/if}
