<script lang="ts">
	interface Props {
		title: string;
		class?: string;
	}

	let { title, class: klass = '' }: Props = $props();
</script>

<h2 class="mb-4 text-xl font-bold text-fg sm:mb-6 sm:text-2xl {klass}">{title}</h2>
