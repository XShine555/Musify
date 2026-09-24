import { page } from '$app/state';
import type { LucideIcon } from '@lucide/svelte';
import Home from '@lucide/svelte/icons/house';
import Compass from '@lucide/svelte/icons/compass';
import ListMusic from '@lucide/svelte/icons/list-music';
import Heart from '@lucide/svelte/icons/heart';
import Disc from '@lucide/svelte/icons/disc-2';
import Folder from '@lucide/svelte/icons/folder';
import Upload from '@lucide/svelte/icons/upload';
import type { SessionUser } from '$lib/types';

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

export function isNavActive(href: string): boolean {
	const section: string | null | undefined = page.data.section;
	if (section !== undefined) return section === href;
	return href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href);
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
