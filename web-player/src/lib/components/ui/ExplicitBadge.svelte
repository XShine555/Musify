<script lang="ts">
	interface Props {
		class?: string;
	}

	let { class: klass = '' }: Props = $props();
</script>

<span
	class="grid h-4 w-4 shrink-0 place-items-center rounded-sm bg-fg-3 text-[11px] leading-4 font-bold text-bg {klass}"
	title="Contenido explícito"
	aria-label="Contenido explícito">E</span
>
