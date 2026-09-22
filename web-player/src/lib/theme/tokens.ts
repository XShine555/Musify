function r(value: number, digits = 4): string {
	return value.toFixed(digits);
}

function tc(alpha: number, l: number, hue: number): string {
	const lightness = 0.37 - l / 380;
	const chroma = Math.max(0, 0.15 - l / 1600);
	return `oklch(${r(lightness)} ${r(chroma)} ${r(hue, 1)} / ${alpha})`;
}

function tcLight(alpha: number, l: number, hue: number): string {
	const depth = (88 - l) / 58;
	const lightness = 0.9 - 0.14 * depth;
	const chroma = 0.05 + 0.13 * depth;
	return `oklch(${r(lightness)} ${r(chroma)} ${r(hue, 1)} / ${alpha})`;
}

export interface ThemeTokens {
	[cssVar: string]: string;
}

export function buildThemeTokens(hue: number, mode: 'dark' | 'light' = 'dark'): ThemeTokens {
	const h = ((hue % 360) + 360) % 360;
	const h2 = (h + 40) % 360;
	const H = r(h, 1);
	const H2 = r(h2, 1);
	const light = mode === 'light';

	const accentL = light ? 0.52 : 0.74;
	const accentC = light ? 0.16 : 0.09;
	const titleL = light ? 0.4 : 0.87;
	const titleC = light ? 0.13 : 0.05;
	const mutedL = light ? 0.48 : 0.79;
	const mutedC = light ? 0.06 : 0.035;

	return {
		'--mf-accent': `oklch(${r(accentL)} ${r(accentC)} ${H})`,
		'--mf-accent-title': `oklch(${r(titleL)} ${r(titleC)} ${H})`,
		'--mf-accent-muted': `oklch(${r(mutedL)} ${r(mutedC)} ${H})`,
		'--mf-accent-soft-bg': `color-mix(in srgb, oklch(${r(accentL)} ${r(accentC)} ${H}) 8%, var(--mf-bg))`,
		'--mf-accent-line': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.2)`,
		'--mf-accent-hair': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.09)`,
		'--mf-accent-btn-bg': `color-mix(in srgb, oklch(${r(accentL)} ${r(accentC)} ${H}) 15%, var(--mf-bg))`,
		'--mf-accent-btn-bg-hover': `color-mix(in srgb, oklch(${r(accentL)} ${r(accentC)} ${H}) 24%, var(--mf-bg))`,
		'--mf-bg': light ? `oklch(0.97 0.014 ${H})` : `oklch(0.075 0.02 ${H})`,
		'--mf-elevated': light ? `oklch(0.99 0.008 ${H})` : `oklch(0.12 0.025 ${H})`,
		'--mf-panel-bg': light ? `oklch(0.97 0.014 ${H} / 0.7)` : `oklch(0.1 0.022 ${H} / 0.28)`,
		'--mf-bar-bg': light ? `oklch(0.97 0.014 ${H} / 0.9)` : `oklch(0.115 0.025 ${H} / 0.9)`,
		'--mf-hairline': light ? `oklch(0.3 0.035 ${H} / 0.1)` : `oklch(0.62 0.05 ${H} / 0.08)`,
		'--mf-ambient': light
			? 'none'
			: `radial-gradient(95% 85% at 20% 0%, ${tc(0.12, 30, h)}, transparent 88%), radial-gradient(90% 80% at 80% 0%, ${tc(0.09, 40, h)}, transparent 88%)`,
		'--mf-hero-tint': light ? tcLight(1, 55, h) : tc(1, 42, h),
		'--mf-hero-bg': light
			? `linear-gradient(${tcLight(0.08, 55, h)}, ${tcLight(0.08, 55, h)})`
			: `linear-gradient(${tc(0.1, 38, h)}, ${tc(0.1, 38, h)})`,
		'--mf-modal-glow': light
			? `linear-gradient(135deg, ${tcLight(0.34, 42, h)} 0%, ${tcLight(0.2, 55, h)} 30%, ${tcLight(0.1, 70, h)} 55%, ${tcLight(0.04, 85, h)} 80%, ${tcLight(0.02, 88, h)} 100%)`
			: `linear-gradient(135deg, ${tc(0.34, 42, h)} 0%, ${tc(0.11, 70, h)} 28%, ${tc(0.03, 88, h)} 50%, rgba(6,6,9,0) 70%)`,
		'--mf-spotlight-bg': light
			? `linear-gradient(${tcLight(0.08, 55, h)}, ${tcLight(0.08, 55, h)})`
			: `linear-gradient(${tc(0.1, 38, h)}, ${tc(0.1, 38, h)})`,
		'--mf-logo-grad': light
			? `linear-gradient(140deg, oklch(0.74 0.18 ${H}), oklch(0.58 0.16 ${H2}) 92%)`
			: `linear-gradient(140deg, oklch(0.78 0.15 ${H}), oklch(0.62 0.13 ${H2}) 92%)`,
		'--mf-cover-grad': light
			? `linear-gradient(150deg, oklch(0.7 0.16 ${H}), oklch(0.54 0.11 ${H}))`
			: `linear-gradient(150deg, oklch(0.5 0.13 ${H}), oklch(0.16 0.035 ${H}))`
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
