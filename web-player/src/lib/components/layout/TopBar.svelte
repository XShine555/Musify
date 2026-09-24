<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import type { SessionUser } from '$lib/types';
	import { SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import { searchHref } from '$lib/utils/hrefs';
	import Search from '@lucide/svelte/icons/search';
	import X from '@lucide/svelte/icons/x';
	import ChevronLeft from '@lucide/svelte/icons/chevron-left';
	import AccountMenu from '$lib/components/ui/overlay/AccountMenu.svelte';
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
	let searchInput: HTMLInputElement | undefined = $state();
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	function runSearch(term: string) {
		if (term.length > 0 && term.length < SEARCH_MIN_LENGTH) return;
		goto(searchHref(term), {
			replaceState: page.url.pathname === '/explore',
			keepFocus: true,
			noScroll: true
		});
	}

	function onInput() {
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => runSearch(query.trim()), SEARCH_DEBOUNCE_MS);
	}

	function clearQuery() {
		query = '';
		clearTimeout(searchTimeout);
		runSearch('');
		searchInput?.focus();
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
		<label
			class="flex h-9.5 w-full items-center gap-2.5 rounded-control bg-surface-2 px-3.5 transition focus-within:bg-surface-hover"
		>
			<Search class="size-icon-sm shrink-0 text-fg-2" />
			<input
				type="text"
				bind:this={searchInput}
				bind:value={query}
				oninput={onInput}
				placeholder="Canciones, artistas, álbumes o playlists"
				class="w-full min-w-0 bg-transparent text-sm text-fg placeholder:text-fg-2 focus:outline-none"
			/>
			{#if query}
				<button
					type="button"
					onclick={clearQuery}
					aria-label="Borrar búsqueda"
					class="grid size-5 shrink-0 place-items-center rounded-full text-fg-2 transition hover:text-fg"
				>
					<X class="size-icon-xs" />
				</button>
			{/if}
		</label>
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
