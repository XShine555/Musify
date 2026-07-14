export const HUES = [145, 330, 40, 152, 300, 200, 10, 120, 280];

export function accentForHue(hue: number): string {
	return `oklch(72% 0.17 ${hue})`;
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
