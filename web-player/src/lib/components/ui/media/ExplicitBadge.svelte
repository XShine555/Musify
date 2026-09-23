<script lang="ts">
	interface Props {
		class?: string;
	}

	let { class: klass = '' }: Props = $props();
</script>

<span
	class="grid size-4 shrink-0 place-items-center rounded-tag border border-accent/30 bg-accent-tint pt-px text-xs leading-none font-normal text-accent-soft {klass}"
	title="Contenido explícito"
	aria-label="Contenido explícito">E</span
>
