<script lang="ts">
	import type { Snippet } from 'svelte';
	import '@fontsource-variable/sora/index.css';
	import '@fontsource-variable/plus-jakarta-sans/index.css';
	import '$lib/theme/theme.css';
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import DialogHost from '$lib/components/ui/overlay/DialogHost.svelte';
	import { ACCENT_HUE } from '$lib/theme/color';
	import { buildThemeTokens, tokensToCss } from '$lib/theme/tokens';
	import { accent, animateThemeHue } from '$lib/theme/accent.svelte';
	import { themeMode } from '$lib/theme/mode.svelte';

	interface Props {
		children: Snippet;
	}

	let { children }: Props = $props();

	const initialThemeCss = `:root{${tokensToCss(buildThemeTokens(ACCENT_HUE, 'dark'))}}:root[data-theme='light']{${tokensToCss(buildThemeTokens(ACCENT_HUE, 'light'))}}`;

	$effect(() => animateThemeHue(accent.hue, themeMode.current));
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<!-- eslint-disable-next-line svelte/no-at-html-tags -- CSS generado a partir de números, sin entrada de usuario -->
	{@html `<style>${initialThemeCss}</style>`}
</svelte:head>

{@render children()}

<DialogHost />
