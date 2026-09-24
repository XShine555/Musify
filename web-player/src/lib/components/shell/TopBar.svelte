<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import type { SessionUser } from '$lib/types';
	import { SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import { searchHref } from '$lib/utils/hrefs';
	import ChevronLeft from '@lucide/svelte/icons/chevron-left';
	import AccountMenu from '$lib/components/shell/AccountMenu.svelte';
	import SearchInput from '$lib/components/ui/primitives/SearchInput.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';

	interface Props {
		user: SessionUser | null;
		accountUrl?: string | null;
	}

	let { user, accountUrl }: Props = $props();

	let query = $derived(
		page.url.pathname === '/explore' ? (page.url.searchParams.get('q') ?? '') : ''
	);
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	function runSearch(term: string) {
		if (term.length > 0 && term.length < SEARCH_MIN_LENGTH) return;
		goto(searchHref(term), {
			replaceState: page.url.pathname === '/explore',
			keepFocus: true,
			noScroll: true
		});
	}

	function oninput() {
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => runSearch(query.trim()), SEARCH_DEBOUNCE_MS);
	}

	function onclear() {
		clearTimeout(searchTimeout);
		runSearch('');
	}

	function onSubmit(event: SubmitEvent) {
		event.preventDefault();
		clearTimeout(searchTimeout);
		runSearch(query.trim());
	}
</script>

<header class="hidden items-center gap-3.5 px-6.5 pt-4 pb-3 sm:flex">
	<IconButton label="Atrás" shape="round" surface size="xs" onclick={() => history.back()}>
		<ChevronLeft class="size-icon-sm" />
	</IconButton>

	<form onsubmit={onSubmit} class="max-w-search min-w-0 flex-1">
		<SearchInput
			bind:value={query}
			placeholder="Canciones, artistas, álbumes o playlists"
			{oninput}
			{onclear}
		/>
	</form>

	<div class="ml-auto shrink-0">
		{#if user}
			<AccountMenu {user} {accountUrl} />
		{:else}
			<Button href="/auth/login" reload variant="accent" size="sm" class="shrink-0"
				>Iniciar sesión</Button
			>
		{/if}
	</div>
</header>
