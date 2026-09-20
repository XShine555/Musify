import { ACCENT_CHROMA, ACCENT_LIGHTNESS } from './color';

const L = ACCENT_LIGHTNESS / 100;
const C = ACCENT_CHROMA;

function r(value: number, digits = 4): string {
	return value.toFixed(digits);
}

function tc(alpha: number, l: number, hue: number): string {
	const lightness = 0.34 - l / 380;
	const chroma = Math.max(0, 0.11 - l / 1600);
	return `oklch(${r(lightness)} ${r(chroma)} ${r(hue, 1)} / ${alpha})`;
}

// Light-mode counterpart of tc(): fades toward a soft tinted glass (~0.88
// lightness) instead of toward black, so the "glow" gradients read as a
// visible colored tint instead of a dark smudge — or, at the old lightness
// floor of 0.97, an almost invisible wash next to the near-white canvas.
function tcLight(alpha: number, l: number, hue: number): string {
	const depth = (88 - l) / 58;
	const lightness = 0.88 - 0.16 * depth;
	const chroma = 0.03 + 0.11 * depth;
	return `oklch(${r(lightness)} ${r(chroma)} ${r(hue, 1)} / ${alpha})`;
}

export interface ThemeTokens {
	[cssVar: string]: string;
}

/**
 * Every color in the app derives from the hue of the track in playback.
 * See design_handoff_musify_player/README.md § "Sistema de color dinámico".
 *
 * `mode` only affects the neutral canvas tokens (bg/elevated/panel/hairline):
 * these are re-applied as inline styles on every hue change, so a light-mode
 * override in theme.css alone can never win against them — the light values
 * have to come from here instead.
 */
export function buildThemeTokens(hue: number, mode: 'dark' | 'light' = 'dark'): ThemeTokens {
	const h = ((hue % 360) + 360) % 360;
	const h2 = (h + 40) % 360;
	const H = r(h, 1);
	const H2 = r(h2, 1);
	const light = mode === 'light';

	// Dark mode wants a bright, light accent (pops on a near-black canvas).
	// Light mode needs the opposite: the same hue pulled down to a dark,
	// saturated shade so it still reads against a near-white canvas — using
	// the dark-mode lightness here is what made accent text/icons/buttons
	// nearly invisible in light mode.
	const accentL = light ? 0.52 : L;
	const accentC = light ? 0.16 : C;
	const titleL = light ? 0.4 : 0.87;
	const titleC = light ? 0.13 : 0.07;
	const mutedL = light ? 0.48 : 0.79;
	const mutedC = light ? 0.06 : 0.035;

	return {
		'--mf-accent': `oklch(${r(accentL)} ${r(accentC)} ${H})`,
		'--mf-accent-title': `oklch(${r(titleL)} ${r(titleC)} ${H})`,
		'--mf-accent-muted': `oklch(${r(mutedL)} ${r(mutedC)} ${H})`,
		'--mf-accent-soft-bg': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.08)`,
		'--mf-accent-line': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.2)`,
		'--mf-accent-hair': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.09)`,
		'--mf-accent-btn-bg': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.15)`,
		'--mf-accent-btn-bg-hover': `oklch(${r(accentL)} ${r(accentC)} ${H} / 0.24)`,
		'--mf-bg': light ? `oklch(0.97 0.006 ${H})` : `oklch(0.068 0.008 ${H})`,
		'--mf-elevated': light ? `oklch(0.99 0.003 ${H})` : `oklch(0.11 0.012 ${H})`,
		'--mf-panel-bg': light ? `oklch(0.97 0.006 ${H} / 0.7)` : `oklch(0.09 0.01 ${H} / 0.28)`,
		'--mf-bar-bg': light ? `oklch(0.97 0.006 ${H} / 0.9)` : `oklch(0.105 0.012 ${H} / 0.9)`,
		'--mf-hairline': light ? `oklch(0.3 0.02 ${H} / 0.1)` : `oklch(0.62 0.03 ${H} / 0.08)`,
		'--mf-ambient': light
			? 'none'
			: `radial-gradient(52% 38% at 14% 0%, ${tc(0.26, 30, h)}, transparent 54%), radial-gradient(46% 34% at 78% 0%, ${tc(0.2, 40, h)}, transparent 56%)`,
		'--mf-hero-bg': light
			? `linear-gradient(${tcLight(0.16, 55, h)}, ${tcLight(0.16, 55, h)})`
			: `linear-gradient(105deg, ${tc(0.34, 42, h)} 0%, ${tc(0.11, 70, h)} 37%, ${tc(0.03, 88, h)} 65%, rgba(6,6,9,0) 92%)`,
		'--mf-modal-glow': light
			? `linear-gradient(135deg, ${tcLight(0.34, 42, h)} 0%, ${tcLight(0.2, 55, h)} 30%, ${tcLight(0.1, 70, h)} 55%, ${tcLight(0.04, 85, h)} 80%, ${tcLight(0.02, 88, h)} 100%)`
			: `linear-gradient(135deg, ${tc(0.34, 42, h)} 0%, ${tc(0.11, 70, h)} 28%, ${tc(0.03, 88, h)} 50%, rgba(6,6,9,0) 70%)`,
		'--mf-spotlight-bg': light
			? `linear-gradient(${tcLight(0.16, 55, h)}, ${tcLight(0.16, 55, h)})`
			: `linear-gradient(100deg, ${tc(0.28, 46, h)} 0%, ${tc(0.1, 74, h)} 40%, ${tc(0.03, 88, h)} 69%, rgba(6,6,9,0) 96%)`,
		'--mf-logo-grad': light
			? `linear-gradient(140deg, oklch(0.68 0.16 ${H}), oklch(0.5 0.14 ${H2}) 92%)`
			: `linear-gradient(140deg, oklch(0.78 0.15 ${H}), oklch(0.62 0.13 ${H2}) 92%)`,
		'--mf-cover-grad': light
			? `linear-gradient(150deg, oklch(0.62 0.14 ${H}), oklch(0.44 0.08 ${H}))`
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
