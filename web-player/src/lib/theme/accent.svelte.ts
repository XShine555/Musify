import { browser } from '$app/environment';
import { buildThemeTokens, applyThemeTokens } from './tokens';
import { extractAccentHue } from './palette';
import { ACCENT_HUE } from './color';

const FADE_MIN_MS = 2000;
const FADE_EXTRA_MS = 2000;

class AccentState {
	hue = $state<number>(ACCENT_HUE);
	#cache = new Map<string, number>();

	async follow(coverUrl: string, isStillCurrent: () => boolean) {
		const cached = this.#cache.get(coverUrl);
		if (cached !== undefined) {
			this.hue = cached;
			return;
		}
		const extracted = await extractAccentHue(coverUrl);
		if (!isStillCurrent()) return;
		if (extracted !== null) this.#cache.set(coverUrl, extracted);
		this.hue = extracted ?? ACCENT_HUE;
	}
}

export const accent = new AccentState();

let displayedHue: number = ACCENT_HUE;

export function animateThemeHue(targetHue: number, mode: 'dark' | 'light'): () => void {
	if (!browser) return () => {};
	const fromHue = displayedHue;
	let delta = targetHue - fromHue;
	if (delta > 180) delta -= 360;
	else if (delta < -180) delta += 360;
	const start = performance.now();
	const duration = FADE_MIN_MS + Math.random() * FADE_EXTRA_MS;
	let rafId = 0;
	const tick = (now: number) => {
		const t = Math.min(1, (now - start) / duration);
		const eased = 1 - Math.pow(1 - t, 3);
		const hue = (((fromHue + delta * eased) % 360) + 360) % 360;
		displayedHue = hue;
		applyThemeTokens(buildThemeTokens(hue, mode));
		if (t < 1) rafId = requestAnimationFrame(tick);
	};
	rafId = requestAnimationFrame(tick);
	return () => cancelAnimationFrame(rafId);
}
