<script lang="ts">
	import { untrack } from 'svelte';
	import { enhance } from '$app/forms';
	import Button from '$lib/components/ui/primitives/Button.svelte';

	interface Props {
		following: boolean;
		invalidateAll?: boolean;
		class?: string;
	}

	let { following: initial, invalidateAll = false, class: klass = '' }: Props = $props();

	let following = $state(untrack(() => initial));
	let submitting = $state(false);
</script>

<form
	method="POST"
	action={following ? '?/unfollow' : '?/follow'}
	class={klass}
	use:enhance={() => {
		submitting = true;
		return async ({ result, update }) => {
			if (result.type === 'success' && typeof result.data?.following === 'boolean')
				following = result.data.following;
			await update({ reset: false, invalidateAll });
			submitting = false;
		};
	}}
>
	<Button
		type="submit"
		size="sm"
		variant={following ? 'secondary' : 'primary'}
		disabled={submitting}
	>
		{following ? 'Dejar de seguir' : 'Seguir'}
	</Button>
</form>
