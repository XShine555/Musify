<script lang="ts">
	import Users from '@lucide/svelte/icons/users';
	import Lock from '@lucide/svelte/icons/lock';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/primitives/InfiniteScroll.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import UserRow from '$lib/components/ui/media/UserRow.svelte';
	import { appendUnique } from '$lib/data/collections';
	import { plural } from '$lib/utils/format';

	let { data, form } = $props();

	type ListUser = NonNullable<typeof data.users>['items'][number];

	let items = $state<ListUser[]>([]);
	let page = $state(1);
	let hasMore = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		items = data.users?.items ?? [];
		page = Number(data.users?.pageNumber ?? 1);
		hasMore = data.users?.hasNextPage ?? false;
	});

	const isFollowers = $derived(data.list === 'followers');
	const title = $derived(isFollowers ? 'Seguidores' : 'Siguiendo');
	const total = $derived(Number(data.users?.totalItemCount ?? 0));

	async function loadMore() {
		if (loadingMore) return;
		loadingMore = true;
		try {
			const res = await fetch(`/api/users/${data.profile.id}/${data.list}?pageNumber=${page + 1}`);
			if (!res.ok) throw new Error(String(res.status));
			const next = (await res.json()) as NonNullable<typeof data.users>;
			items = appendUnique(items, next.items, (u) => u.id);
			page = Number(next.pageNumber);
			hasMore = next.hasNextPage;
		} catch {
			hasMore = false;
		} finally {
			loadingMore = false;
		}
	}
</script>

<svelte:head>
	<title>{title} · {data.profile.name}</title>
</svelte:head>

<Page>
	<PageHeader
		eyebrow={data.profile.name}
		{title}
		meta={data.forbidden
			? undefined
			: isFollowers
				? plural(total, 'seguidor', 'seguidores')
				: plural(total, 'usuario', 'usuarios')}
	/>

	{#if form?.message}
		<Alert class="mb-4">{form.message}</Alert>
	{/if}

	{#if data.forbidden}
		<EmptyState
			icon={Lock}
			title="Seguidores privados"
			description="Solo puedes ver los seguidores de {data.profile.name} si os seguís mutuamente."
		/>
	{:else if items.length === 0}
		<EmptyState
			icon={Users}
			title={isFollowers ? 'Sin seguidores' : 'No sigue a nadie'}
			description={isFollowers
				? `Todavía nadie sigue a ${data.profile.name}.`
				: `${data.profile.name} todavía no sigue a nadie.`}
		/>
	{:else}
		<ul class="flex flex-col gap-1">
			{#each items as user (user.id)}
				<li>
					<UserRow {user} />
				</li>
			{/each}
		</ul>
		<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
	{/if}
</Page>
