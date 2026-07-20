import { env } from '$env/dynamic/private';

const DAY_SECONDS = 60 * 60 * 24;
const DEFAULT_OIDC_SCOPE = 'openid profile email offline_access';

function positiveInt(value: string | undefined, fallback: number): number {
	if (value === undefined || value === '') return fallback;
	const parsed = Number(value);
	return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

export const authConfig = {
	get issuer() {
		return env.ZITADEL_ISSUER;
	},
	get clientId() {
		return env.ZITADEL_CLIENT_ID;
	},
	get clientSecret(): string | undefined {
		const secret = env.ZITADEL_CLIENT_SECRET;
		return secret && !secret.startsWith('REPLACE_') ? secret : undefined;
	},
	get scope() {
		return env.OIDC_SCOPE || DEFAULT_OIDC_SCOPE;
	},
	get redirectUri() {
		return env.AUTH_REDIRECT_URI;
	},
	get postLogoutUri() {
		return env.AUTH_POST_LOGOUT_URI;
	},
	get sessionSecret() {
		return env.SESSION_SECRET;
	},
	get sessionTtlSeconds() {
		return positiveInt(env.SESSION_TTL_SECONDS, 7 * DAY_SECONDS);
	},
	get refreshThresholdSeconds() {
		return positiveInt(env.SESSION_REFRESH_THRESHOLD_SECONDS, 30);
	},
	get flowTtlSeconds() {
		return positiveInt(env.AUTH_FLOW_TTL_SECONDS, 600);
	},
	get allowInsecureHttp() {
		return env.AUTH_ALLOW_INSECURE_HTTP === 'true';
	}
};

export const apiConfig = {
	get baseUrl() {
		return (env.API_BASE_URL ?? '').replace(/\/+$/, '');
	}
};

export const thumbnailConfig = {
	get maxAgeSeconds() {
		return positiveInt(env.THUMBNAIL_CACHE_MAX_AGE_SECONDS, 30 * DAY_SECONDS);
	},
	get failureCacheSeconds() {
		return positiveInt(env.THUMBNAIL_FAILURE_CACHE_SECONDS, 300);
	},
	get memoryCacheBytes() {
		return positiveInt(env.THUMBNAIL_CACHE_BYTES, 128 * 1024 * 1024);
	},
	get maxConcurrentFetches() {
		return positiveInt(env.THUMBNAIL_MAX_CONCURRENT_FETCHES, 4);
	},
	get fetchTimeoutMs() {
		return positiveInt(env.THUMBNAIL_FETCH_TIMEOUT_MS, 8000);
	},
	get cooldownSeconds() {
		return positiveInt(env.THUMBNAIL_COOLDOWN_SECONDS, 30);
	},
	get maxCooldownSeconds() {
		return positiveInt(env.THUMBNAIL_MAX_COOLDOWN_SECONDS, 300);
	},
	get maxSize() {
		return positiveInt(env.THUMBNAIL_MAX_SIZE, 720);
	}
};
