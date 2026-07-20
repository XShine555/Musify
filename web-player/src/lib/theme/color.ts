export const HUES = [145, 330, 40, 152, 300, 200, 10, 120, 280];

export const ACCENT_LIGHTNESS = 60;
export const ACCENT_CHROMA = 0.17;
export const ACCENT_HUE = 145;

export function hueFor(id: string | number): number {
	const text = String(id);
	let hash = 0;
	for (let i = 0; i < text.length; i++) hash = (hash * 31 + text.charCodeAt(i)) >>> 0;
	return HUES[hash % HUES.length];
}

export function accentForHue(hue: number): string {
	return `oklch(${ACCENT_LIGHTNESS}% ${ACCENT_CHROMA} ${hue})`;
}

export function gradientForHue(hue: number): string {
	return `linear-gradient(135deg, oklch(70% 0.15 ${hue}), oklch(36% 0.13 ${hue + 28}))`;
}
