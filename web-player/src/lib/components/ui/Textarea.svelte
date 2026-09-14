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
	class="min-h-0 w-full resize-none rounded-control bg-surface px-4 py-3 text-sm text-fg transition placeholder:text-muted focus:bg-surface-2 focus:outline-none {klass}"
></textarea>
