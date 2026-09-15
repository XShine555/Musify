<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import type { SessionUser } from '$lib/types';
	import { SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import Search from '@lucide/svelte/icons/search';
	import X from '@lucide/svelte/icons/x';
	import ChevronLeft from '@lucide/svelte/icons/chevron-left';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import LogOut from '@lucide/svelte/icons/log-out';
	import User from '@lucide/svelte/icons/user';
	import Settings from '@lucide/svelte/icons/settings';
	import Sun from '@lucide/svelte/icons/sun';
	import Moon from '@lucide/svelte/icons/moon';
	import MenuItem from './ui/MenuItem.svelte';
	import GlassMenu from './ui/GlassMenu.svelte';
	import { themeMode } from '$lib/theme/mode.svelte';

	interface Props {
		user: SessionUser | null;
		accountUrl?: string | null;
	}

	let { user, accountUrl }: Props = $props();

	let query = $derived(
		page.url.pathname === '/explore' ? (page.url.searchParams.get('q') ?? '') : ''
	);
	let menuOpen = $state(false);
	let menuRef: HTMLDivElement | undefined = $state();
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

	function onDocumentClick(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) menuOpen = false;
	}
</script>

<header class="hidden items-center gap-3.5 px-6.5 pt-4 pb-3 sm:flex">
	<div class="flex shrink-0 gap-1.5">
		<button
			type="button"
			onclick={() => history.back()}
			aria-label="Atrás"
			class="grid h-7.5 w-7.5 place-items-center rounded-full bg-white/[3.5%] text-fg-2 transition-colors hover:bg-white/7 hover:text-fg"
		>
			<ChevronLeft class="h-[15px] w-[15px]" strokeWidth={1.9} />
		</button>
		<span
			aria-hidden="true"
			class="grid h-7.5 w-7.5 place-items-center rounded-full bg-white/[3.5%] text-muted"
		>
			<ChevronRight class="h-[15px] w-[15px]" strokeWidth={1.9} />
		</span>
	</div>

	<form onsubmit={onSubmit} class="max-w-[480px] min-w-0 flex-1">
		<label
			class="flex h-9.5 w-full items-center gap-2.5 rounded-[11px] bg-white/[3.5%] px-3.5 transition focus-within:bg-white/[6%]"
		>
			<Search class="h-[15px] w-[15px] shrink-0 text-fg-3" strokeWidth={1.8} />
			<input
				type="text"
				bind:this={searchInput}
				bind:value={query}
				oninput={onInput}
				placeholder="Canciones, artistas, álbumes o playlists"
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

	<div class="flex-1"></div>

	<div class="relative shrink-0" bind:this={menuRef}>
		{#if user}
			<button
				type="button"
				onclick={() => (menuOpen = !menuOpen)}
				aria-label="Tu cuenta"
				class="block h-8.25 w-8.25 shrink-0 overflow-hidden rounded-full border border-white/10 transition active:scale-95"
			>
				{#if user.picture}
					<img src={user.picture} alt="" class="h-full w-full object-cover opacity-90" />
				{:else}
					<span
						class="grid h-full w-full place-items-center bg-surface-2 text-xs font-semibold text-fg uppercase"
					>
						{user.name.charAt(0)}
					</span>
				{/if}
			</button>
			{#if menuOpen}
				<GlassMenu class="absolute top-full right-0 z-20 mt-2 w-52 overflow-hidden">
					<MenuItem
						icon={User}
						label="Ver perfil"
						href="/u/{user.sub}"
						onclick={() => (menuOpen = false)}
					/>
					{#if accountUrl}
						<MenuItem
							icon={Settings}
							label="Ajustes"
							href={accountUrl}
							target="_blank"
							onclick={() => (menuOpen = false)}
						/>
					{/if}
					<MenuItem
						icon={themeMode.current === 'dark' ? Sun : Moon}
						label={themeMode.current === 'dark' ? 'Modo blanco' : 'Modo oscuro'}
						onclick={() => themeMode.toggle()}
					/>
					<form method="POST" action="/logout" data-sveltekit-reload>
						<MenuItem icon={LogOut} label="Cerrar sesión" type="submit" />
					</form>
				</GlassMenu>
			{/if}
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

<svelte:window onclick={onDocumentClick} />
