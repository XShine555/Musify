<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import type { SessionUser } from '$lib/types';
	import { SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import Search from '@lucide/svelte/icons/search';
	import X from '@lucide/svelte/icons/x';
	import ChevronLeft from '@lucide/svelte/icons/chevron-left';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import AccountMenu from './ui/AccountMenu.svelte';
	import Avatar from './ui/Avatar.svelte';

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

	function buildHref(term: string) {
		return term ? `/explore?q=${encodeURIComponent(term)}` : '/explore';
	}

	function runSearch(term: string) {
		if (term.length > 0 && term.length < SEARCH_MIN_LENGTH) return;
		goto(buildHref(term), {
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
	<div class="flex shrink-0 gap-1.5">
		<button
			type="button"
			onclick={() => history.back()}
			aria-label="Atrás"
			class="grid h-7.5 w-7.5 place-items-center rounded-full bg-surface-2 text-fg-2 transition-colors hover:bg-surface-hover hover:text-fg"
		>
			<ChevronLeft class="h-3.75 w-3.75" strokeWidth={1.9} />
		</button>
		<span
			aria-hidden="true"
			class="grid h-7.5 w-7.5 place-items-center rounded-full bg-surface-2 text-muted"
		>
			<ChevronRight class="h-3.75 w-3.75" strokeWidth={1.9} />
		</span>
	</div>

	<form onsubmit={onSubmit} class="max-w-120 min-w-0 flex-1">
		<label
			class="flex h-9.5 w-full items-center gap-2.5 rounded-control bg-surface-2 px-3.5 transition focus-within:bg-surface-hover"
		>
			<Search class="h-3.75 w-3.75 shrink-0 text-fg-3" strokeWidth={1.8} />
			<input
				type="text"
				bind:this={searchInput}
				bind:value={query}
				oninput={onInput}
				placeholder="Canciones, artistas, álbumes o playlists"
				class="w-full min-w-0 bg-transparent text-sm text-fg placeholder:text-muted focus:outline-none"
			/>
			{#if query}
				<button
					type="button"
					onclick={clearQuery}
					aria-label="Borrar búsqueda"
					class="grid h-5 w-5 shrink-0 place-items-center rounded-full text-muted transition hover:text-fg"
				>
					<X class="h-3.5 w-3.5" />
				</button>
			{/if}
		</label>
	</form>

	<div class="flex-1"></div>

	<div class="relative shrink-0">
		{#if user}
			<AccountMenu
				{user}
				{accountUrl}
				panelClass="absolute top-full right-0 z-20 mt-2 w-52 overflow-hidden"
			>
				{#snippet trigger({ toggle, open })}
					<button
						type="button"
						onclick={toggle}
						aria-label="Tu cuenta"
						aria-expanded={open}
						class="block shrink-0 rounded-full transition active:scale-95"
					>
						<Avatar
							name={user.name}
							src={user.picture}
							size="sm"
							class="border border-line-strong opacity-90"
						/>
					</button>
				{/snippet}
			</AccountMenu>
		{:else}
			<a
				href="/auth/login"
				data-sveltekit-reload
				class="shrink-0 rounded-full bg-accent-soft px-4 py-2 text-sm font-semibold text-ink transition hover:brightness-110"
			>
				Iniciar sesión
			</a>
		{/if}
	</div>
</header>
