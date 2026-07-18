<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		children: Snippet;
	}

	let { class: klass = '', children }: Props = $props();
</script>

<section class="page-x pt-11 pb-10 {klass}">
	{@render children()}
</section>
