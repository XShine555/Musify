<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import Search from '@lucide/svelte/icons/search';
	import X from '@lucide/svelte/icons/x';

	interface Props {
		class?: string;
		placeholder?: string;
	}

	let { class: klass = '', placeholder = 'Canciones, artistas, álbumes o playlists' }: Props =
		$props();

	let query = $state('');
	let searchInput: HTMLInputElement | undefined = $state();
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	function navigateSearch() {
		const term = query.trim();
		goto(term ? `/explore?q=${encodeURIComponent(term)}` : '/explore', {
			replaceState: page.url.pathname === '/explore',
			keepFocus: true,
			noScroll: true
		});
	}

	function onInput() {
		clearTimeout(searchTimeout);
		const term = query.trim();
		const onExplore = page.url.pathname === '/explore';
		if (term.length > 0 && term.length < SEARCH_MIN_LENGTH) return;
		if (term.length === 0 && !onExplore) return;
		searchTimeout = setTimeout(navigateSearch, SEARCH_DEBOUNCE_MS);
	}

	function clearQuery() {
		clearTimeout(searchTimeout);
		query = '';
		if (page.url.pathname === '/explore') navigateSearch();
		searchInput?.focus();
	}

	function onSubmit(event: SubmitEvent) {
		event.preventDefault();
		clearTimeout(searchTimeout);
		navigateSearch();
	}

	function onKeydown(event: KeyboardEvent) {
		if (event.key !== 'Enter') return;
		event.preventDefault();
		clearTimeout(searchTimeout);
		navigateSearch();
	}
</script>

<div class="relative min-w-0 {klass}">
	<form onsubmit={onSubmit}>
		<label
			class="flex h-9.5 w-full items-center gap-2.5 rounded-[11px] bg-white/[3.5%] px-3.5 transition focus-within:bg-white/[6%]"
		>
			<Search class="h-[15px] w-[15px] shrink-0 text-fg-3" strokeWidth={1.8} />
			<input
				type="text"
				bind:this={searchInput}
				bind:value={query}
				oninput={onInput}
				onkeydown={onKeydown}
				{placeholder}
				class="w-full min-w-0 bg-transparent text-[13px] text-fg placeholder:text-muted focus:outline-none"
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
</div>
