import { thumbnailConfig } from '$lib/server/config';

const ALLOWED_HOST_SUFFIXES = ['.googleusercontent.com', '.ytimg.com', '.ggpht.com'];

export interface ThumbnailImage {
	body: ArrayBuffer;
	contentType: string;
	etag: string;
}

interface CacheEntry {
	image: ThumbnailImage | null;
	expiresAt: number;
}

const cache = new Map<string, CacheEntry>();
const inFlight = new Map<string, Promise<ThumbnailImage | null>>();

let cachedBytes = 0;
let cooldownUntil = 0;
let active = 0;
const waiting: (() => void)[] = [];

export function parseThumbnailUrl(rawUrl: string): URL | null {
	let url: URL;
	try {
		url = new URL(rawUrl);
	} catch {
		return null;
	}

	if (url.protocol !== 'https:') return null;
	if (!ALLOWED_HOST_SUFFIXES.some((suffix) => url.hostname.endsWith(suffix))) return null;

	return url;
}

export function resizeThumbnailUrl(url: URL, size: number): string {
	if (url.hostname.endsWith('.googleusercontent.com')) {
		const raw = url.toString();
		const separator = raw.lastIndexOf('=');
		const base =
			separator > 0 && /^[wsh]\d/.test(raw.slice(separator + 1)) ? raw.slice(0, separator) : raw;
		return `${base}=w${size}-h${size}-l90-rj`;
	}

	if (url.hostname.endsWith('.ytimg.com')) {
		const path = url.origin + url.pathname;
		const lastSlash = path.lastIndexOf('/');
		if (lastSlash < 0) return path;
		return `${path.slice(0, lastSlash)}/${ytImgFileName(size)}`;
	}

	return url.toString();
}

function ytImgFileName(size: number): string {
	if (size <= 90) return 'default.jpg';
	if (size <= 180) return 'mqdefault.jpg';
	if (size <= 360) return 'hqdefault.jpg';
	return 'maxresdefault.jpg';
}

export async function loadThumbnail(url: URL, size: number): Promise<ThumbnailImage | null> {
	const target = resizeThumbnailUrl(url, size);

	const cached = cache.get(target);
	if (cached && cached.expiresAt > Date.now()) {
		touch(target, cached);
		return cached.image;
	}

	const pending = inFlight.get(target);
	if (pending) return pending;

	const request = fetchThumbnail(target).finally(() => inFlight.delete(target));
	inFlight.set(target, request);
	return request;
}

async function fetchThumbnail(target: string): Promise<ThumbnailImage | null> {
	if (Date.now() < cooldownUntil) return null;

	await acquire();
	try {
		const response = await fetch(target, {
			headers: { Accept: 'image/webp,image/avif,image/*;q=0.8' },
			signal: AbortSignal.timeout(thumbnailConfig.fetchTimeoutMs)
		});

		if (response.status === 429 || response.status === 503) {
			cooldownUntil = Date.now() + retryAfterMs(response);
			store(target, null, thumbnailConfig.failureCacheSeconds);
			return null;
		}

		if (!response.ok) {
			store(target, null, thumbnailConfig.failureCacheSeconds);
			return null;
		}

		const buffer = await response.arrayBuffer();
		const image: ThumbnailImage = {
			body: buffer,
			contentType: response.headers.get('content-type') ?? 'image/jpeg',
			etag: `W/"${buffer.byteLength.toString(16)}-${hash(target).toString(16)}"`
		};

		store(target, image, thumbnailConfig.maxAgeSeconds);
		return image;
	} catch {
		store(target, null, thumbnailConfig.failureCacheSeconds);
		return null;
	} finally {
		release();
	}
}

function retryAfterMs(response: Response): number {
	const header = Number(response.headers.get('retry-after'));
	const seconds = Number.isFinite(header) && header > 0 ? header : thumbnailConfig.cooldownSeconds;
	return Math.min(seconds, thumbnailConfig.maxCooldownSeconds) * 1000;
}

function store(target: string, image: ThumbnailImage | null, ttlSeconds: number) {
	const previous = cache.get(target);
	if (previous?.image) cachedBytes -= previous.image.body.byteLength;

	cache.delete(target);
	cache.set(target, { image, expiresAt: Date.now() + ttlSeconds * 1000 });
	if (image) cachedBytes += image.body.byteLength;

	evict();
}

function touch(target: string, entry: CacheEntry) {
	cache.delete(target);
	cache.set(target, entry);
}

function evict() {
	while (cachedBytes > thumbnailConfig.memoryCacheBytes) {
		const oldest = cache.keys().next();
		if (oldest.done) return;

		const entry = cache.get(oldest.value);
		if (entry?.image) cachedBytes -= entry.image.body.byteLength;
		cache.delete(oldest.value);
	}
}

function acquire(): Promise<void> {
	if (active < thumbnailConfig.maxConcurrentFetches) {
		active++;
		return Promise.resolve();
	}
	return new Promise((resolve) => waiting.push(resolve));
}

function release() {
	const next = waiting.shift();
	if (next) next();
	else active--;
}

function hash(value: string): number {
	let result = 2166136261;
	for (let i = 0; i < value.length; i++) {
		result ^= value.charCodeAt(i);
		result = Math.imul(result, 16777619);
	}
	return result >>> 0;
}
