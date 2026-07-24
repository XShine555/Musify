<script lang="ts">
	import '@fontsource-variable/inter/index.css';
	import '@fontsource-variable/sora/index.css';
	import '$lib/theme/theme.css';
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import PlayerBar from '$lib/components/player/PlayerBar.svelte';
	import Sidebar from '$lib/components/Sidebar.svelte';
	import MobileHeader from '$lib/components/MobileHeader.svelte';
	import MobileNav from '$lib/components/MobileNav.svelte';
	import { player } from '$lib/player/player.svelte';
	import { ACCENT_LIGHTNESS, ACCENT_CHROMA, ACCENT_HUE } from '$lib/theme/color';
	import { trackNavigation } from '$lib/navigation.svelte';
	import { page } from '$app/state';

	let { children, data } = $props();

	trackNavigation();

	const isAuthPage = $derived(page.url.pathname === '/login');
	const hasTrack = $derived(player.currentId !== null);

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

	$effect(() => {
		document.documentElement.classList.toggle('home', page.url.pathname === '/');
	});

	function parseAccent(value: string): { c: number; h: number } | null {
		const m = /oklch\(\s*[\d.]+%?\s+([\d.]+)\s+([\d.]+)/.exec(value);
		return m ? { c: parseFloat(m[1]), h: parseFloat(m[2]) } : null;
	}

	let displayed = { c: ACCENT_CHROMA, h: ACCENT_HUE };
	let rafId = 0;

	$effect(() => {
		const target = parseAccent(player.accent);
		if (!target) {
			document.documentElement.style.setProperty('--mf-accent', player.accent);
			return;
		}
		cancelAnimationFrame(rafId);
		const from = { ...displayed };
		let dh = target.h - from.h;
		if (dh > 180) dh -= 360;
		else if (dh < -180) dh += 360;
		const start = performance.now();
		const duration = 350;
		const tick = (now: number) => {
			const t = Math.min(1, (now - start) / duration);
			const e = 1 - Math.pow(1 - t, 3);
			const c = from.c + (target.c - from.c) * e;
			const h = (((from.h + dh * e) % 360) + 360) % 360;
			displayed = { c, h };
			document.documentElement.style.setProperty(
				'--mf-accent',
				`oklch(${ACCENT_LIGHTNESS}% ${c.toFixed(3)} ${h.toFixed(1)})`
			);
			if (t < 1) rafId = requestAnimationFrame(tick);
		};
		rafId = requestAnimationFrame(tick);
		return () => cancelAnimationFrame(rafId);
	});
</script>

<svelte:window onkeydown={onWindowKeydown} />

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

{#if isAuthPage || !data.user}
	<div class="min-h-screen bg-bg text-fg antialiased">
		{@render children()}
	</div>
{:else}
	<div class="flex min-h-screen bg-bg text-fg antialiased">
		<Sidebar user={data.user} />
		<div class="flex min-w-0 flex-1 flex-col">
			<MobileHeader user={data.user} />
			<main
				class="flex-1 transition-[padding]"
				style="padding-bottom:calc(var(--mf-nav-h) + var(--mf-safe-b) + {hasTrack
					? 'var(--mf-player-h)'
					: '0px'})"
			>
				{#key page.url.pathname}
					<div class="animate-fade">{@render children()}</div>
				{/key}
			</main>
			{#if hasTrack}
				<PlayerBar />
			{/if}
			<MobileNav />
		</div>
	</div>
{/if}
