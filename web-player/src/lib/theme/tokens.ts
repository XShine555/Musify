function r(value: number, digits = 4): string {
	return value.toFixed(digits);
}

export interface ThemeTokens {
	[cssVar: string]: string;
}

export function buildThemeTokens(hue: number, mode: 'dark' | 'light' = 'dark'): ThemeTokens {
	const h = ((hue % 360) + 360) % 360;
	const H = r(h, 1);
	const light = mode === 'light';

	const accentL = light ? 0.5 : 0.72;
	const accentC = light ? 0.07 : 0.06;
	const titleL = light ? 0.36 : 0.86;
	const titleC = light ? 0.06 : 0.03;
	const mutedL = light ? 0.42 : 0.8;
	const mutedC = light ? 0.05 : 0.02;
	const accent = `oklch(${r(accentL)} ${r(accentC)} ${H})`;

	const g = (l1: number, c1: number, a1: number, l2: number, c2: number, a2: number) =>
		`linear-gradient(90deg, oklch(${l1} ${c1} ${H} / ${a1}), oklch(${l2} ${c2} ${H} / ${a2}))`;
	const gActive = light
		? g(0.86, 0.04, 0.45, 0.9, 0.03, 0.28)
		: g(0.46, 0.06, 0.34, 0.4, 0.045, 0.22);
	const gHover = light
		? g(0.9, 0.03, 0.34, 0.93, 0.02, 0.2)
		: g(0.42, 0.045, 0.26, 0.38, 0.035, 0.17);
	const glow = (l: number, c: number, a: number) => `oklch(${l} ${c} ${H} / ${a})`;
	// Fixed danger hue so error dialogs read red whatever the theme hue is.
	const dangerGlow = (l: number, c: number, a: number) => `oklch(${l} ${c} 25 / ${a})`;

	return {
		'--mf-accent': accent,
		'--mf-accent-title': `oklch(${r(titleL)} ${r(titleC)} ${H})`,
		'--mf-accent-muted': `oklch(${r(mutedL)} ${r(mutedC)} ${H})`,
		'--mf-accent-soft-bg': `color-mix(in srgb, ${accent} 12%, var(--mf-bg))`,
		'--mf-accent-line': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.2)`,
		'--mf-accent-hair': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.09)`,
		'--mf-accent-btn-bg': `color-mix(in srgb, ${accent} 18%, var(--mf-bg))`,
		'--mf-accent-btn-bg-hover': `color-mix(in srgb, ${accent} 26%, var(--mf-bg))`,
		'--mf-g-active': gActive,
		'--mf-g-hover': gHover,
		'--mf-bg': light ? `oklch(0.975 0.012 ${H})` : `oklch(0.068 0.008 ${H})`,
		'--mf-elevated': light ? `oklch(0.99 0.008 ${H})` : `oklch(0.12 0.012 ${H})`,
		'--mf-panel-bg': light ? 'rgba(255, 255, 255, 0.28)' : 'rgba(0, 0, 0, 0.16)',
		'--mf-hairline': light ? `oklch(0.3 0.02 ${H} / 0.06)` : `oklch(0.62 0.02 ${H} / 0.05)`,
		'--mf-ambient': light
			? `radial-gradient(80% 70% at 12% 0%, ${glow(0.9, 0.05, 0.55)}, transparent 75%), radial-gradient(70% 60% at 88% 8%, ${glow(0.92, 0.035, 0.4)}, transparent 75%), radial-gradient(90% 70% at 50% 110%, ${glow(0.93, 0.03, 0.35)}, transparent 80%), linear-gradient(180deg, ${glow(0.96, 0.02, 0.4)}, transparent 60%)`
			: `radial-gradient(75% 65% at 12% 0%, ${glow(0.3, 0.045, 0.5)}, transparent 75%), radial-gradient(65% 55% at 85% 5%, ${glow(0.24, 0.035, 0.35)}, transparent 75%), radial-gradient(90% 70% at 50% 105%, ${glow(0.18, 0.025, 0.3)}, transparent 80%), linear-gradient(180deg, ${glow(0.14, 0.02, 0.35)}, transparent 55%)`,
		'--mf-slider-fill': light
			? `linear-gradient(90deg, oklch(0.5 0.1 ${H}), oklch(0.68 0.11 ${r(h + 18, 1)}), oklch(0.5 0.1 ${H}))`
			: `linear-gradient(90deg, oklch(0.66 0.11 ${H}), oklch(0.84 0.09 ${r(h + 18, 1)}), oklch(0.66 0.11 ${H}))`,
		'--mf-hero-tint': light ? `oklch(0.8 0.05 ${H})` : `oklch(0.3 0.04 ${H})`,
		'--mf-hero-bg': light
			? `linear-gradient(120deg, ${glow(0.9, 0.04, 0.6)}, ${glow(0.93, 0.03, 0.52)} 60%)`
			: `linear-gradient(120deg, ${glow(0.24, 0.04, 0.55)}, ${glow(0.14, 0.02, 0.35)} 60%, ${glow(0.12, 0.015, 0.25)})`,
		'--mf-modal-glow': light
			? `linear-gradient(135deg, ${glow(0.88, 0.05, 0.5)} 0%, ${glow(0.93, 0.03, 0.2)} 40%, transparent 75%)`
			: `linear-gradient(135deg, ${glow(0.3, 0.045, 0.4)} 0%, ${glow(0.2, 0.03, 0.15)} 35%, transparent 70%)`,
		'--mf-modal-glow-danger': light
			? `linear-gradient(135deg, ${dangerGlow(0.86, 0.07, 0.55)} 0%, ${dangerGlow(0.92, 0.04, 0.22)} 40%, transparent 75%)`
			: `linear-gradient(135deg, ${dangerGlow(0.34, 0.1, 0.45)} 0%, ${dangerGlow(0.22, 0.06, 0.18)} 35%, transparent 70%)`,
		'--mf-spotlight-bg': light
			? `linear-gradient(120deg, ${glow(0.9, 0.04, 0.55)}, ${glow(0.93, 0.03, 0.47)} 55%)`
			: `linear-gradient(120deg, ${glow(0.22, 0.04, 0.5)}, ${glow(0.13, 0.02, 0.3)} 55%, ${glow(0.11, 0.015, 0.2)})`,
		'--mf-cover-grad': light
			? `linear-gradient(150deg, oklch(0.84 0.03 ${H}), oklch(0.74 0.02 ${H}))`
			: `linear-gradient(150deg, oklch(0.34 0.03 ${H}), oklch(0.18 0.015 ${H}))`
	};
}

export function applyThemeTokens(
	tokens: ThemeTokens,
	target: HTMLElement = document.documentElement
) {
	for (const [key, value] of Object.entries(tokens)) target.style.setProperty(key, value);
}

export function tokensToCss(tokens: ThemeTokens): string {
	return Object.entries(tokens)
		.map(([key, value]) => `${key}:${value}`)
		.join(';');
}
