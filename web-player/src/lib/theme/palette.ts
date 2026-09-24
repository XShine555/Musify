import { browser } from '$app/environment';
import { ACCENT_LIGHTNESS } from './color';

function srgbToLinear(channel: number): number {
	const c = channel / 255;
	return c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
}

function rgbToOklch(r: number, g: number, b: number): [number, number, number] {
	const lr = srgbToLinear(r);
	const lg = srgbToLinear(g);
	const lb = srgbToLinear(b);

	const l = 0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb;
	const m = 0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb;
	const s = 0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb;

	const l_ = Math.cbrt(l);
	const m_ = Math.cbrt(m);
	const s_ = Math.cbrt(s);

	const L = 0.2104542553 * l_ + 0.793617785 * m_ - 0.0040720468 * s_;
	const a = 1.9779984951 * l_ - 2.428592205 * m_ + 0.4505937099 * s_;
	const bb = 0.0259040371 * l_ + 0.7827717662 * m_ - 0.808675766 * s_;

	const C = Math.sqrt(a * a + bb * bb);
	const H = (Math.atan2(bb, a) * 180) / Math.PI;
	return [L, C, (H + 360) % 360];
}

function loadImage(url: string): Promise<HTMLImageElement> {
	return new Promise((resolve, reject) => {
		const img = new Image();
		img.crossOrigin = 'anonymous';
		img.onload = () => resolve(img);
		img.onerror = reject;
		img.src = url;
	});
}

const BUCKETS = 36;

export async function extractAccent(url: string): Promise<string | null> {
	if (!browser) return null;

	let img: HTMLImageElement;
	try {
		img = await loadImage(url);
	} catch {
		return null;
	}

	const size = 40;
	const canvas = document.createElement('canvas');
	canvas.width = size;
	canvas.height = size;
	const ctx = canvas.getContext('2d', { willReadFrequently: true });
	if (!ctx) return null;

	ctx.drawImage(img, 0, 0, size, size);
	let pixels: Uint8ClampedArray;
	try {
		pixels = ctx.getImageData(0, 0, size, size).data;
	} catch {
		return null;
	}

	const weight = new Array(BUCKETS).fill(0);
	const hueSin = new Array(BUCKETS).fill(0);
	const hueCos = new Array(BUCKETS).fill(0);
	const chromaSum = new Array(BUCKETS).fill(0);

	for (let i = 0; i < pixels.length; i += 4) {
		if (pixels[i + 3] < 128) continue;
		const [L, C, H] = rgbToOklch(pixels[i], pixels[i + 1], pixels[i + 2]);
		if (L < 0.2 || L > 0.92 || C < 0.03) continue;

		const bucket = Math.floor((H / 360) * BUCKETS) % BUCKETS;
		const w = C * C;
		const rad = (H * Math.PI) / 180;
		weight[bucket] += w;
		hueSin[bucket] += Math.sin(rad) * w;
		hueCos[bucket] += Math.cos(rad) * w;
		chromaSum[bucket] += C * w;
	}

	let best = -1;
	let bestWeight = 0;
	for (let b = 0; b < BUCKETS; b++) {
		if (weight[b] > bestWeight) {
			bestWeight = weight[b];
			best = b;
		}
	}
	if (best < 0) return null;

	const hue = ((Math.atan2(hueSin[best], hueCos[best]) * 180) / Math.PI + 360) % 360;
	const chroma = Math.min(0.11, Math.max(0.045, chromaSum[best] / weight[best]));

	const h = hue.toFixed(1);
	const c = chroma.toFixed(3);

	return `oklch(${ACCENT_LIGHTNESS}% ${c} ${h})`;
}
