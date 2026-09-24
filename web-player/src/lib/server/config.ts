import { env } from '$env/dynamic/private';

const DAY_SECONDS = 60 * 60 * 24;
const MIN_SESSION_SECRET_LENGTH = 32;
const DEFAULT_OIDC_SCOPE = 'openid profile email offline_access';

function positiveInt(value: string | undefined, fallback: number): number {
	if (value === undefined || value === '') return fallback;
	const parsed = Number(value);
	return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

function required(name: string, value: string | undefined): string {
	if (!value) throw new Error(`Missing required environment variable: ${name}`);
	return value;
}

export const authConfig = {
	get issuer() {
		return required('ZITADEL_ISSUER', env.ZITADEL_ISSUER);
	},
	get clientId() {
		return required('ZITADEL_CLIENT_ID', env.ZITADEL_CLIENT_ID);
	},
	get clientSecret(): string | undefined {
		const secret = env.ZITADEL_CLIENT_SECRET;
		return secret && !secret.startsWith('REPLACE_') ? secret : undefined;
	},
	get scope() {
		return env.OIDC_SCOPE || DEFAULT_OIDC_SCOPE;
	},
	get redirectUri() {
		return required('AUTH_REDIRECT_URI', env.AUTH_REDIRECT_URI);
	},
	get postLogoutUri() {
		return required('AUTH_POST_LOGOUT_URI', env.AUTH_POST_LOGOUT_URI);
	},
	get sessionSecret() {
		const secret = required('SESSION_SECRET', env.SESSION_SECRET);
		if (secret.length < MIN_SESSION_SECRET_LENGTH) {
			throw new Error(
				`SESSION_SECRET must be at least ${MIN_SESSION_SECRET_LENGTH} characters long.`
			);
		}
		return secret;
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
