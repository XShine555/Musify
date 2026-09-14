import { browser } from '$app/environment';

export type ThemeName = 'dark' | 'light';

const STORAGE_KEY = 'musify.theme';

function loadInitial(): ThemeName {
	if (!browser) return 'dark';
	try {
		return localStorage.getItem(STORAGE_KEY) === 'light' ? 'light' : 'dark';
	} catch {
		return 'dark';
	}
}

function applyTheme(theme: ThemeName) {
	if (!browser) return;
	document.documentElement.dataset.theme = theme;
}

class ThemeModeStore {
	current = $state<ThemeName>(loadInitial());

	constructor() {
		applyTheme(this.current);
	}

	toggle() {
		this.current = this.current === 'dark' ? 'light' : 'dark';
		applyTheme(this.current);
		if (!browser) return;
		try {
			localStorage.setItem(STORAGE_KEY, this.current);
		} catch {
			/* localStorage unavailable (private mode, quota) — preference just won't persist */
		}
	}
}

export const themeMode = new ThemeModeStore();
