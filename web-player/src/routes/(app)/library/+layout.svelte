<script lang="ts" module>
	const SECTIONS = [
		{
			href: '/library',
			label: 'Canciones',
			description: 'Tus canciones subidas. Puedes reproducirlas o borrarlas desde aquí.'
		},
		{
			href: '/library/albums',
			label: 'Álbumes',
			description: 'Crea álbumes, EPs y singles con las canciones de tu biblioteca.'
		},
		{
			href: '/library/uploads',
			label: 'Subidas',
			description: 'Sube varias canciones a la vez y añádelas a tu biblioteca.'
		}
	];
</script>

<script lang="ts">
	import { page } from '$app/state';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Tabs from '$lib/components/ui/primitives/Tabs.svelte';

	let { children } = $props();

	const pathname = $derived(page.url.pathname.replace(/\/$/, ''));
	const current = $derived(
		SECTIONS.find(
			(section) =>
				section.href !== '/library' &&
				(pathname === section.href || pathname.startsWith(`${section.href}/`))
		) ?? SECTIONS[0]
	);
	const tabs = $derived(
		SECTIONS.map(({ href, label }) => ({ href, label, active: href === current.href }))
	);
</script>

<svelte:head>
	<title>Gestionar música · {current.label}</title>
	<meta name="description" content={current.description} />
</svelte:head>

<Page>
	<PageHeader title="Gestionar música" description={current.description} />
	<Tabs label="Secciones de la biblioteca" {tabs} />
	{#key page.url.pathname}
		<div class="animate-fade">{@render children()}</div>
	{/key}
</Page>
