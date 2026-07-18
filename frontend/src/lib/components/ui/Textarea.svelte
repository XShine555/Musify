<script lang="ts">
	interface Props {
		value?: string;
		id?: string;
		name?: string;
		placeholder?: string;
		maxlength?: number;
		class?: string;
	}

	let {
		value = $bindable(''),
		id,
		name,
		placeholder,
		maxlength,
		class: klass = ''
	}: Props = $props();
</script>

<textarea
	{id}
	{name}
	{placeholder}
	{maxlength}
	bind:value
	class="w-full min-h-0 resize-none rounded-control border border-line bg-surface px-4 py-3 text-base text-fg transition placeholder:text-muted focus:border-accent/50 focus:outline-none {klass}"
></textarea>
