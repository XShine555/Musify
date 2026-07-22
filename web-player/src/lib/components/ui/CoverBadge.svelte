<script lang="ts">
	interface Props {
		label: string;
		class?: string;
	}

	let { label, class: klass = '' }: Props = $props();
</script>

<span
	class="pointer-events-none absolute top-2 left-2 rounded-full bg-black/45 px-2 py-0.5 text-[10px] font-semibold tracking-[0.08em] text-white uppercase backdrop-blur-sm {klass}"
>
	{label}
</span>
