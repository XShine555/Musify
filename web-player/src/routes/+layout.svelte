<script lang="ts">
	import '@fontsource-variable/sora/index.css';
	import '@fontsource-variable/plus-jakarta-sans/index.css';
	import '$lib/theme/theme.css';
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import PlayerDock from '$lib/components/player/PlayerDock.svelte';
	import Queue from '$lib/components/player/Queue.svelte';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';
	import TopBar from '$lib/components/layout/TopBar.svelte';
	import MobileHeader from '$lib/components/layout/MobileHeader.svelte';
	import Modal from '$lib/components/ui/overlay/Modal.svelte';
	import DialogHost from '$lib/components/ui/overlay/DialogHost.svelte';
	import PlaylistForm from '$lib/components/ui/forms/PlaylistForm.svelte';
	import { player } from '$lib/player/player.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { createPlaylistModal } from '$lib/state/playlists.svelte';
	import { ACCENT_HUE } from '$lib/theme/color';
	import { buildThemeTokens, applyThemeTokens, tokensToCss } from '$lib/theme/tokens';
	import { themeMode } from '$lib/theme/mode.svelte';
	import { trackNavigation, restoreScroll } from '$lib/state/navigation.svelte';
	import { page } from '$app/state';

	let { children, data } = $props();

	let scroller = $state<HTMLElement>();

	trackNavigation();
	restoreScroll(() => scroller);

	$effect(() => {
		if (data.likedTracks?.length) liked.hydrate(data.likedTracks);
	});

	$effect(() => {
		if (data.lastPlayedTrack) player.hydrate(data.lastPlayedTrack);
	});

	const isAuthPage = $derived(page.url.pathname === '/auth');

	const initialThemeCss = `:root{${tokensToCss(buildThemeTokens(ACCENT_HUE, 'dark'))}}:root[data-theme='light']{${tokensToCss(buildThemeTokens(ACCENT_HUE, 'light'))}}`;

	let queueAutoOpened = false;
	$effect(() => {
		if (!player.playing || queueAutoOpened) return;
		queueAutoOpened = true;
		queuePanel.show();
	});

	function isTypingTarget(target: EventTarget | null): boolean {
		if (!(target instanceof HTMLElement)) return false;
		if (target.isContentEditable) return true;
		return ['INPUT', 'TEXTAREA', 'SELECT', 'BUTTON', 'A'].includes(target.tagName);
	}

	function onWindowKeydown(event: KeyboardEvent) {
		if (event.code !== 'Space' || isTypingTarget(event.target)) return;
		event.preventDefault();
		player.toggle();
	}

	function parseHue(value: string): number | null {
		const m = /oklch\(\s*[\d.]+%?\s+[\d.]+\s+([\d.]+)/.exec(value);
		return m ? parseFloat(m[1]) : null;
	}

	let displayedHue = ACCENT_HUE;
	let rafId = 0;

	$effect(() => {
		const targetHue = parseHue(player.accent) ?? ACCENT_HUE;
		const mode = themeMode.current;
		cancelAnimationFrame(rafId);
		const fromHue = displayedHue;
		let dh = targetHue - fromHue;
		if (dh > 180) dh -= 360;
		else if (dh < -180) dh += 360;
		const start = performance.now();
		const duration = 2000 + Math.random() * 2000;
		const tick = (now: number) => {
			const t = Math.min(1, (now - start) / duration);
			const eased = 1 - Math.pow(1 - t, 3);
			const hue = (((fromHue + dh * eased) % 360) + 360) % 360;
			displayedHue = hue;
			applyThemeTokens(buildThemeTokens(hue, mode));
			if (t < 1) rafId = requestAnimationFrame(tick);
		};
		rafId = requestAnimationFrame(tick);
		return () => cancelAnimationFrame(rafId);
	});
</script>

<svelte:window onkeydown={onWindowKeydown} />

<svelte:head>
	<link rel="icon" href={favicon} />
	<!-- eslint-disable-next-line svelte/no-at-html-tags -- CSS generado a partir de números, sin entrada de usuario -->
	{@html `<style>${initialThemeCss}</style>`}
</svelte:head>

{#if isAuthPage || (!data.user && !data.allowAnonymousListening)}
	<div class="min-h-screen bg-bg text-fg antialiased">
		{@render children()}
	</div>
{:else}
	<div class="relative flex h-dvh overflow-hidden text-fg antialiased">
		<div class="app-backdrop"></div>
		<div class="app-vignette"></div>

		<Sidebar
			user={data.user}
			playlists={data.userPlaylists ?? []}
			playlistCount={data.userPlaylistsTotal ?? 0}
		/>

		<div class="flex min-w-0 flex-1 flex-col">
			<div class="flex min-h-0 flex-1">
				<div bind:this={scroller} class="flex min-w-0 flex-1 app-scroll flex-col">
					<MobileHeader user={data.user} accountUrl={data.accountUrl} />
					<TopBar user={data.user} accountUrl={data.accountUrl} />
					<main class="flex-1">
						{#key page.url.pathname}
							<div class="animate-fade">{@render children()}</div>
						{/key}
					</main>
				</div>

				<Queue />
			</div>

			<PlayerDock user={data.user} />
		</div>

		<Modal
			open={createPlaylistModal.open}
			onClose={() => createPlaylistModal.close()}
			title="Crear nueva playlist"
		>
			<PlaylistForm
				action="/playlists?/create"
				formMessage={page.form?.message}
				submitLabel="Crear"
				submittingLabel="Creando…"
				helperText="Podrás añadir canciones más tarde."
				onCancel={() => createPlaylistModal.close()}
				onSuccess={() => createPlaylistModal.close()}
			/>
		</Modal>
	</div>
{/if}

<DialogHost />
