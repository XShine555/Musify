import { browser } from '$app/environment';
import { readStorage, writeStorage } from '$lib/utils/storage';

export type ThemeName = 'dark' | 'light';

const STORAGE_KEY = 'musify.theme';

function loadInitial(): ThemeName {
	if (!browser) return 'dark';
	return readStorage(STORAGE_KEY) === 'light' ? 'light' : 'dark';
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
		writeStorage(STORAGE_KEY, this.current);
	}
}

export const themeMode = new ThemeModeStore();
