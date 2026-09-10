import type { SessionUser } from '$lib/types';

declare global {
	namespace App {
		interface Locals {
			user: SessionUser | null;
			accessToken: string | null;
		}

		interface PageData {
			section?: string | null;
			allowAnonymousListening?: boolean;
		}
	}
}

export {};
