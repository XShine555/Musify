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

export function accentSoft(hue: number, pct: number): string {
	return `color-mix(in srgb, ${accentForHue(hue)} ${pct}%, transparent)`;
}

export function gradientForHue(hue: number): string {
	return `linear-gradient(135deg, oklch(70% 0.15 ${hue}), oklch(36% 0.13 ${hue + 28}))`;
}

export function fmtTime(seconds: number): string {
	const total = Math.max(0, Math.floor(seconds || 0));
	const minutes = Math.floor(total / 60);
	const rest = total % 60;
	return `${minutes}:${rest < 10 ? '0' : ''}${rest}`;
}

const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

export function fmtDate(value: string | number | Date): string {
	return dateFormatter.format(new Date(value));
}
