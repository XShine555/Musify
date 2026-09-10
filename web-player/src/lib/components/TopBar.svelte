<script lang="ts">
	import { goto } from '$app/navigation';
	import type { SessionUser, YouTubeSong } from '$lib/types';
	import { player } from '$lib/player/player.svelte';
	import {
		queueItemForTarget,
		targetArtist,
		targetCoverSrc,
		targetExplicit,
		targetId,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';
	import { SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import Search from '@lucide/svelte/icons/search';
	import X from '@lucide/svelte/icons/x';
	import LogOut from '@lucide/svelte/icons/log-out';
	import MenuItem from './ui/MenuItem.svelte';
	import Cover from './ui/Cover.svelte';
	import ExplicitBadge from './ui/ExplicitBadge.svelte';

	interface Props {
		user: SessionUser | null;
	}

	const QUICK_RESULTS_LIMIT = 5;

	interface QuickSearchTrack {
		id: string;
		title: string;
		artist?: string | null;
		isExplicit?: boolean;
	}

	interface QuickSearchResponse {
		items: { track?: QuickSearchTrack | null; youTubeSong?: YouTubeSong | null }[];
	}

	let { user }: Props = $props();

	let query = $state('');
	let results = $state<TrackTarget[]>([]);
	let resultsOpen = $state(false);
	let searching = $state(false);
	let menuOpen = $state(false);
	let menuRef: HTMLDivElement | undefined = $state();
	let searchRef: HTMLDivElement | undefined = $state();
	let searchInput: HTMLInputElement | undefined = $state();
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;
	let searchToken = 0;

	function clearQuery() {
		query = '';
		results = [];
		resultsOpen = false;
		searchInput?.focus();
	}

	function onInput() {
		clearTimeout(searchTimeout);
		const term = query.trim();
		if (term.length < SEARCH_MIN_LENGTH) {
			results = [];
			resultsOpen = false;
			return;
		}
		resultsOpen = true;
		searchTimeout = setTimeout(() => runQuickSearch(term), SEARCH_DEBOUNCE_MS);
	}

	async function runQuickSearch(term: string) {
		const token = ++searchToken;
		searching = true;
		try {
			const params = new URLSearchParams({ name: term, pageSize: String(QUICK_RESULTS_LIMIT) });
			const res = await fetch(`/api/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			const data = (await res.json()) as QuickSearchResponse;
			if (token !== searchToken) return;
			results = data.items
				.flatMap((item): TrackTarget[] => {
					if (item.track) return [{ kind: 'local', track: item.track }];
					if (item.youTubeSong) return [{ kind: 'youtube', song: item.youTubeSong }];
					return [];
				})
				.slice(0, QUICK_RESULTS_LIMIT);
		} catch {
			if (token === searchToken) results = [];
		} finally {
			if (token === searchToken) searching = false;
		}
	}

	function playResult(index: number) {
		player.playOrToggle(results.map(queueItemForTarget), index);
		resultsOpen = false;
	}

	function onSubmit(event: SubmitEvent) {
		event.preventDefault();
		clearTimeout(searchTimeout);
		resultsOpen = false;
		const q = query.trim();
		goto(q ? `/explore?q=${encodeURIComponent(q)}` : '/explore');
	}

	function onDocumentClick(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) menuOpen = false;
		if (searchRef && !searchRef.contains(event.target as Node)) resultsOpen = false;
	}
</script>

<header
	class="sticky top-0 z-30 hidden grid-cols-[1fr_minmax(0,3fr)_1fr] items-center gap-4 border-b border-line bg-bg/70 px-8 py-4 backdrop-blur-lg md:grid"
>
	<div></div>

	<div class="relative mx-auto w-full max-w-2xl" bind:this={searchRef}>
		<form onsubmit={onSubmit}>
			<label
				class="flex w-full items-center gap-2.5 rounded-full border border-line bg-surface px-4 py-2.5 transition focus-within:border-accent-soft/60"
			>
				<Search class="h-4 w-4 shrink-0 text-fg-2" />
				<input
					type="text"
					bind:this={searchInput}
					bind:value={query}
					oninput={onInput}
					onfocus={() => (resultsOpen = results.length > 0)}
					placeholder="Buscar"
					class="w-full bg-transparent text-sm text-fg placeholder:text-fg-2 focus:outline-none"
				/>
				{#if query}
					<button
						type="button"
						onclick={clearQuery}
						aria-label="Borrar búsqueda"
						class="grid h-5 w-5 shrink-0 place-items-center rounded-full text-fg-2 transition hover:text-fg"
					>
						<X class="h-3.5 w-3.5" />
					</button>
				{/if}
			</label>
		</form>

		{#if resultsOpen && (results.length > 0 || searching)}
			<div
				class="animate-pop absolute top-full right-0 left-0 z-20 mt-2 overflow-hidden rounded-panel border border-line bg-elevated p-1.5 shadow-menu"
			>
				{#if searching && results.length === 0}
					<p class="px-3 py-2.5 text-sm text-fg-2">Buscando…</p>
				{:else}
					{#each results as target, i (targetId(target))}
						<button
							type="button"
							onclick={() => playResult(i)}
							class="group/result flex w-full min-w-0 items-center gap-3 rounded-control p-2 text-left transition hover:bg-hover"
						>
							<Cover
								trackId={targetId(target)}
								src={targetCoverSrc(target)}
								size="small"
								alt={targetTitle(target)}
								class="h-9 w-9 shrink-0 rounded-control shadow-art"
							/>
							<div class="min-w-0 flex-1">
								<div class="flex min-w-0 items-center gap-1.5">
									{#if targetExplicit(target)}
										<ExplicitBadge />
									{/if}
									<span class="truncate text-sm text-fg">{targetTitle(target)}</span>
								</div>
								{#if targetArtist(target)}
									<div class="truncate text-xs text-fg-2">{targetArtist(target)}</div>
								{/if}
							</div>
						</button>
					{/each}
				{/if}
			</div>
		{/if}
	</div>

	<div class="flex items-center justify-end gap-6">
		{#if user}
			<div class="relative" bind:this={menuRef}>
				<button
					type="button"
					onclick={() => (menuOpen = !menuOpen)}
					aria-label="Tu cuenta"
					class="block shrink-0 rounded-full transition active:scale-95"
				>
					{#if user.picture}
						<img src={user.picture} alt="" class="h-9 w-9 rounded-full object-cover" />
					{:else}
						<span
							class="grid h-9 w-9 place-items-center rounded-full bg-surface-2 text-sm font-semibold text-fg uppercase"
						>
							{user.name.charAt(0)}
						</span>
					{/if}
				</button>
				{#if menuOpen}
					<div
						class="animate-pop absolute top-full right-0 z-20 mt-2 w-40 overflow-hidden rounded-panel border border-line bg-elevated p-1.5 shadow-menu"
					>
						<form method="POST" action="/logout" data-sveltekit-reload>
							<MenuItem icon={LogOut} label="Salir" type="submit" />
						</form>
					</div>
				{/if}
			</div>
		{:else}
			<a
				href="/auth/login"
				data-sveltekit-reload
				class="shrink-0 rounded-full bg-accent-soft px-4 py-2 text-sm font-semibold text-on-accent transition hover:brightness-110"
			>
				Iniciar sesión
			</a>
		{/if}
	</div>
</header>

<svelte:window onclick={onDocumentClick} />
