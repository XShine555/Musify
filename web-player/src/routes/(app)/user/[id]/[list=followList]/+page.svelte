<script lang="ts">
	import Users from '@lucide/svelte/icons/users';
	import Lock from '@lucide/svelte/icons/lock';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/primitives/InfiniteScroll.svelte';
	import UserRow from '$lib/components/ui/media/UserRow.svelte';
	import { createPagedList } from '$lib/state/pagedList.svelte';
	import { plural } from '$lib/utils/format';

	let { data } = $props();

	type ListUser = NonNullable<typeof data.users>['items'][number];

	const list = createPagedList<ListUser>(
		async (pageNumber) => {
			const res = await fetch(
				`/api/users/${data.profile.id}/${data.list}?pageNumber=${pageNumber}`
			);
			if (!res.ok) throw new Error(String(res.status));
			return (await res.json()) as NonNullable<typeof data.users>;
		},
		(user) => user.id
	);

	$effect(() => list.reset(data.users));

	const isFollowers = $derived(data.list === 'followers');
	const title = $derived(isFollowers ? 'Seguidores' : 'Siguiendo');
	const total = $derived(data.users?.totalItemCount ?? 0);
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

	{#if data.forbidden}
		<EmptyState
			icon={Lock}
			title="Seguidores privados"
			description="Solo puedes ver los seguidores de {data.profile.name} si os seguís mutuamente."
		/>
	{:else if list.items.length === 0}
		<EmptyState
			icon={Users}
			title={isFollowers ? 'Sin seguidores' : 'No sigue a nadie'}
			description={isFollowers
				? `Todavía nadie sigue a ${data.profile.name}.`
				: `${data.profile.name} todavía no sigue a nadie.`}
		/>
	{:else}
		<ul class="flex flex-col gap-1">
			{#each list.items as user (user.id)}
				<li>
					<UserRow {user} />
				</li>
			{/each}
		</ul>
		<InfiniteScroll
			onLoadMore={() => list.loadMore()}
			hasMore={list.hasMore}
			loading={list.loading}
			error={list.error}
		/>
	{/if}
</Page>
