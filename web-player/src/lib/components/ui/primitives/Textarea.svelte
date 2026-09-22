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
	class="min-h-0 field-control resize-none px-4 {klass}"></textarea>
