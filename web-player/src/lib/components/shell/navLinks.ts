import { page } from '$app/state';
import type { LucideIcon } from '@lucide/svelte';
import { House, Compass, ListMusic, Heart, Music } from '@lucide/svelte';
import type { SessionUser } from '$lib/types';

export interface AppNavLink {
	href: string;
	label: string;
	icon: LucideIcon;
	count?: number;
	inMobileTabs: boolean;
	activeFor?: string[];
}

export interface AppNavSection {
	title?: string;
	links: AppNavLink[];
}

export interface AppNavCounts {
	playlists?: number;
	liked?: number;
}

export function isNavActive(link: AppNavLink): boolean {
	return [link.href, ...(link.activeFor ?? [])].some(isPathActive);
}

function isPathActive(href: string): boolean {
	const section: string | null | undefined = page.data.section;
	if (section !== undefined) return section === href;
	return href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href);
}

export function appNavSections(
	user: SessionUser | null,
	counts: AppNavCounts = {}
): AppNavSection[] {
	if (!user)
		return [
			{ links: [{ href: '/explore', label: 'Descubrir', icon: Compass, inMobileTabs: true }] }
		];
	return [
		{
			links: [
				{ href: '/', label: 'Inicio', icon: House, inMobileTabs: true },
				{ href: '/explore', label: 'Descubrir', icon: Compass, inMobileTabs: true },
				{
					href: '/playlists',
					label: 'Playlists',
					icon: ListMusic,
					count: counts.playlists,
					inMobileTabs: true
				},
				{ href: '/liked', label: 'Me gusta', icon: Heart, count: counts.liked, inMobileTabs: true }
			]
		},
		{
			title: 'Estudio',
			links: [
				{
					href: '/library',
					label: 'Gestionar música',
					icon: Music,
					inMobileTabs: false,
					activeFor: ['/albums']
				}
			]
		}
	];
}

export function appNavLinks(user: SessionUser | null, counts: AppNavCounts = {}): AppNavLink[] {
	return appNavSections(user, counts).flatMap((section) => section.links);
}
