<script lang="ts">
	interface Props {
		name?: string | null;
		ownerUserId?: string | number | null;
		class?: string;
	}

	let { name, ownerUserId, class: klass = '' }: Props = $props();
</script>

{#if name}
	{#if ownerUserId}
		<a
			href="/u/{ownerUserId}"
			onclick={(e) => e.stopPropagation()}
			class="block max-w-full truncate hover:text-fg-2 hover:underline {klass}"
		>
			{name}
		</a>
	{:else}
		<span class="block truncate {klass}">{name}</span>
	{/if}
{/if}
