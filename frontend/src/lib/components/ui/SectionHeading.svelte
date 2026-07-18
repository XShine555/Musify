<script lang="ts">
	interface Props {
		title: string;
		class?: string;
	}

	let { title, class: klass = '' }: Props = $props();
</script>

<h2 class="mb-6 text-2xl font-bold text-fg {klass}">{title}</h2>
