<script lang="ts">
	interface Props {
		name?: string | null;
		ownerUserId?: string | null;
		coauthors?: { id: string; name: string }[];
		class?: string;
	}

	let { name, ownerUserId, coauthors = [], class: klass = '' }: Props = $props();
</script>

{#snippet person(label: string, id?: string | null)}
	{#if id}
		<a href="/user/{id}" onclick={(e) => e.stopPropagation()} class="hover:underline">{label}</a>
	{:else}
		{label}
	{/if}
{/snippet}

{#if name}
	{#if coauthors.length > 0}
		<span class="block max-w-full truncate {klass}">
			{@render person(name, ownerUserId)}{#each coauthors as coauthor (coauthor.id)}, {@render person(
					coauthor.name,
					coauthor.id
				)}{/each}
		</span>
	{:else if ownerUserId}
		<a
			href="/user/{ownerUserId}"
			onclick={(e) => e.stopPropagation()}
			class="block max-w-full truncate hover:underline {klass}"
		>
			{name}
		</a>
	{:else}
		<span class="block truncate {klass}">{name}</span>
	{/if}
{/if}
