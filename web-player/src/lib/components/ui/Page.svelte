<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		children: Snippet;
	}

	let { class: klass = '', children }: Props = $props();
</script>

<section class="page-x pt-7 pb-8 sm:pt-11 sm:pb-10 {klass}">
	{@render children()}
</section>
