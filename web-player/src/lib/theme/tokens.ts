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

// Light-mode counterpart of tc(): fades toward the light canvas (~0.97
// lightness, 0 chroma at l=88) instead of toward black, so the "glow"
// gradients read as a soft tint instead of a dark smudge on a light bg.
function tcLight(alpha: number, l: number, hue: number): string {
	const depth = (88 - l) / 58;
	const lightness = 0.97 - 0.07 * depth;
	const chroma = Math.max(0, 0.06 * depth);
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

	return {
		'--mf-accent': `oklch(${r(L)} ${r(C)} ${H})`,
		'--mf-accent-title': `oklch(0.87 0.07 ${H})`,
		'--mf-accent-muted': `oklch(0.79 0.035 ${H})`,
		'--mf-accent-soft-bg': `oklch(${r(L)} ${r(C)} ${H} / 0.08)`,
		'--mf-accent-line': `oklch(${r(L)} ${r(C)} ${H} / 0.2)`,
		'--mf-accent-hair': `oklch(${r(L)} ${r(C)} ${H} / 0.09)`,
		'--mf-accent-btn-bg': `oklch(${r(L)} ${r(C)} ${H} / 0.15)`,
		'--mf-accent-btn-bg-hover': `oklch(${r(L)} ${r(C)} ${H} / 0.24)`,
		'--mf-bg': light ? `oklch(0.97 0.006 ${H})` : `oklch(0.068 0.008 ${H})`,
		'--mf-elevated': light ? `oklch(0.99 0.003 ${H})` : `oklch(0.11 0.012 ${H})`,
		'--mf-sidebar-bg': light
			? `linear-gradient(180deg, oklch(0.985 0.004 ${H}), oklch(0.965 0.006 ${H}))`
			: `linear-gradient(180deg, oklch(0.095 0.012 ${H}), oklch(0.068 0.007 ${H}))`,
		'--mf-panel-bg': light ? `oklch(0.97 0.006 ${H} / 0.7)` : `oklch(0.09 0.01 ${H} / 0.28)`,
		'--mf-bar-bg': light ? `oklch(0.97 0.006 ${H} / 0.9)` : `oklch(0.105 0.012 ${H} / 0.9)`,
		'--mf-hairline': light ? `oklch(0.3 0.02 ${H} / 0.1)` : `oklch(0.62 0.03 ${H} / 0.08)`,
		'--mf-ambient': light
			? `radial-gradient(52% 38% at 14% 0%, ${tcLight(0.26, 30, h)}, transparent 54%), radial-gradient(46% 34% at 78% 0%, ${tcLight(0.2, 40, h)}, transparent 56%)`
			: `radial-gradient(52% 38% at 14% 0%, ${tc(0.26, 30, h)}, transparent 54%), radial-gradient(46% 34% at 78% 0%, ${tc(0.2, 40, h)}, transparent 56%)`,
		'--mf-hero-bg': light
			? `linear-gradient(105deg, ${tcLight(0.34, 42, h)} 0%, ${tcLight(0.11, 70, h)} 37%, ${tcLight(0.03, 88, h)} 65%, rgba(255,255,255,0) 92%)`
			: `linear-gradient(105deg, ${tc(0.34, 42, h)} 0%, ${tc(0.11, 70, h)} 37%, ${tc(0.03, 88, h)} 65%, rgba(6,6,9,0) 92%)`,
		'--mf-modal-glow': light
			? `linear-gradient(135deg, ${tcLight(0.34, 42, h)} 0%, ${tcLight(0.11, 70, h)} 28%, ${tcLight(0.03, 88, h)} 50%, rgba(255,255,255,0) 70%)`
			: `linear-gradient(135deg, ${tc(0.34, 42, h)} 0%, ${tc(0.11, 70, h)} 28%, ${tc(0.03, 88, h)} 50%, rgba(6,6,9,0) 70%)`,
		'--mf-spotlight-bg': light
			? `linear-gradient(100deg, ${tcLight(0.28, 46, h)} 0%, ${tcLight(0.1, 74, h)} 40%, ${tcLight(0.03, 88, h)} 69%, rgba(255,255,255,0) 96%)`
			: `linear-gradient(100deg, ${tc(0.28, 46, h)} 0%, ${tc(0.1, 74, h)} 40%, ${tc(0.03, 88, h)} 69%, rgba(6,6,9,0) 96%)`,
		'--mf-logo-grad': `linear-gradient(140deg, oklch(0.78 0.15 ${H}), oklch(0.62 0.13 ${H2}) 92%)`,
		'--mf-logo-glow': `0 0 20px oklch(${r(L)} ${r(C)} ${H} / 0.3)`,
		'--mf-cover-grad': `linear-gradient(150deg, oklch(0.5 0.13 ${H}), oklch(0.16 0.035 ${H}))`
	};
}

export function applyThemeTokens(
	tokens: ThemeTokens,
	target: HTMLElement = document.documentElement
) {
	for (const [key, value] of Object.entries(tokens)) target.style.setProperty(key, value);
}
