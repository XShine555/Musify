import { afterNavigate } from '$app/navigation';

const HISTORY_INDEX = 'sveltekit:history';

const LABELS: [RegExp, string][] = [
	[/^\/$/, 'Volver al inicio'],
	[/^\/explore\/?$/, 'Volver a explorar'],
	[/^\/playlists\/[^/]+/, 'Volver a la playlist'],
	[/^\/playlists\/?$/, 'Volver a tus playlists'],
	[/^\/albums\/[^/]+/, 'Volver al álbum'],
	[/^\/albums\/?$/, 'Volver a tus álbumes'],
	[/^\/library\/?$/, 'Volver a tus canciones'],
	[/^\/upload\/?$/, 'Volver a subir música'],
	[/^\/mixes\/[^/]+/, 'Volver a la mezcla']
];

export interface PageRef {
	url: string;
	label: string;
}

const visited = new Map<number, PageRef>();
let index = $state(-1);

function labelFor(pathname: string): string | null {
	for (const [pattern, label] of LABELS) {
		if (pattern.test(pathname)) return label;
	}
	return null;
}

export function historyIndex(): number | null {
	const value = history.state?.[HISTORY_INDEX];
	return typeof value === 'number' ? value : null;
}

export function trackNavigation() {
	afterNavigate((nav) => {
		const at = historyIndex();
		if (at === null) return;
		const url = nav.to?.url;
		const label = url ? labelFor(url.pathname) : null;
		if (url && label) visited.set(at, { url: url.pathname + url.search, label });
		else visited.delete(at);
		index = at;
	});
}

export function previousPage(): PageRef | null {
	return visited.get(index - 1) ?? null;
}
