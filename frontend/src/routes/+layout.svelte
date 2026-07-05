<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import Navbar from '$lib/components/Navbar.svelte';
	import Footer from '$lib/components/Footer.svelte';

	let { children, data } = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

<div class="flex min-h-screen flex-col bg-neutral-950 text-neutral-100 antialiased">
	<Navbar user={data.user} />
	<main class="flex-1">{@render children()}</main>
	<Footer />
</div>
