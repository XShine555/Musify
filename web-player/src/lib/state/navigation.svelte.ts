import { afterNavigate } from '$app/navigation';
import type { LucideIcon } from '@lucide/svelte';
import Home from '@lucide/svelte/icons/house';
import Compass from '@lucide/svelte/icons/compass';
import ListMusic from '@lucide/svelte/icons/list-music';
import Heart from '@lucide/svelte/icons/heart';
import Disc from '@lucide/svelte/icons/disc-2';
import Folder from '@lucide/svelte/icons/folder';
import Upload from '@lucide/svelte/icons/upload';
import type { SessionUser } from '../types';

const HISTORY_INDEX = 'sveltekit:history';

const LABELS: [RegExp, string][] = [
	[/^\/$/, 'Volver al inicio'],
	[/^\/explore\/?$/, 'Volver a explorar'],
	[/^\/playlists\/[^/]+/, 'Volver a la lista'],
	[/^\/playlists\/?$/, 'Volver a tus listas'],
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

function historyIndex(): number | null {
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

export function isSectionActive(href: string, pathname: string, section?: string | null): boolean {
	if (section !== undefined) return section === href;
	return href === '/' ? pathname === '/' : pathname.startsWith(href);
}

export function searchHref(query: string): string {
	return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
}

export interface AppNavLink {
	href: string;
	label: string;
	icon: LucideIcon;
	count?: number;
	primary: boolean;
}

export interface AppNavCounts {
	playlists?: number;
	liked?: number;
}

export function appNavLinks(user: SessionUser | null, counts: AppNavCounts = {}): AppNavLink[] {
	if (!user) return [{ href: '/explore', label: 'Descubrir', icon: Compass, primary: true }];
	return [
		{ href: '/', label: 'Inicio', icon: Home, primary: true },
		{ href: '/explore', label: 'Descubrir', icon: Compass, primary: true },
		{
			href: '/playlists',
			label: 'Playlists',
			icon: ListMusic,
			count: counts.playlists,
			primary: true
		},
		{ href: '/liked', label: 'Me gusta', icon: Heart, count: counts.liked, primary: true },
		{ href: '/albums', label: 'Mis álbumes', icon: Disc, primary: false },
		{ href: '/library', label: 'Canciones subidas', icon: Folder, primary: false },
		{ href: '/upload', label: 'Subir música', icon: Upload, primary: false }
	];
}
