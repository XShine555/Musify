<script lang="ts">
	interface Props {
		class?: string;
	}

	let { class: klass = '' }: Props = $props();
</script>

<span
	class="grid size-4 shrink-0 place-items-center rounded-tag bg-fg-3 text-xs leading-4 font-bold text-bg {klass}"
	title="Contenido explícito"
	aria-label="Contenido explícito">E</span
>
