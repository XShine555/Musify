export function readStorage(key: string, fallback: string | null = null): string | null {
	try {
		return localStorage.getItem(key) ?? fallback;
	} catch {
		return fallback;
	}
}

export function writeStorage(key: string, value: string): void {
	try {
		localStorage.setItem(key, value);
	} catch {
		/* localStorage unavailable (private mode, quota) — the value just won't persist */
	}
}
