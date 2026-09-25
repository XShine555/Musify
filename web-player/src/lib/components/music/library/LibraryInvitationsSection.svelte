<script lang="ts">
	import type { SubmitFunction } from '@sveltejs/kit';
	import { flip } from 'svelte/animate';
	import { enhance } from '$app/forms';
	import { invalidateAll } from '$app/navigation';
	import Mail from '@lucide/svelte/icons/mail';
	import type { CoauthorInvitation } from '$lib/types';
	import { fmtDate } from '$lib/utils/format';
	import { collapse } from '$lib/utils/transitions';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import ListRow from '$lib/components/ui/media/ListRow.svelte';

	interface Props {
		invitations: CoauthorInvitation[];
	}

	let { invitations }: Props = $props();

	let hiddenIds = $state<string[]>([]);
	let error = $state('');

	const visible = $derived(invitations.filter((invitation) => !hiddenIds.includes(invitation.id)));

	const respondSubmit: SubmitFunction = ({ formData }) => {
		const id = String(formData.get('invitationId'));
		hiddenIds = [...hiddenIds, id];
		error = '';
		return async ({ result, update }) => {
			if (result.type === 'success') {
				await update({ reset: false });
				hiddenIds = hiddenIds.filter((item) => item !== id);
				return;
			}
			hiddenIds = hiddenIds.filter((item) => item !== id);
			error =
				result.type === 'failure' && typeof result.data?.message === 'string'
					? result.data.message
					: 'No se pudo responder a la invitación.';
			await invalidateAll();
		};
	};
</script>

{#if error}
	<Alert tone="danger" class="mt-4">{error}</Alert>
{/if}

{#if visible.length > 0}
	<ul class="mt-4 flex flex-col">
		{#each visible as invitation, i (invitation.id)}
			<li
				animate:flip={{ duration: 200 }}
				out:collapse
				class="animate-enter"
				style="--i:{Math.min(i, 9)}"
			>
				<ListRow
					trackId={invitation.trackId}
					title={invitation.trackTitle}
					subtitle={invitation.artist}
					subtitleHref={invitation.ownerUserId}
					size="lg"
					class="flex-wrap sm:flex-nowrap"
				>
					{#snippet trailing()}
						<span class="ml-auto hidden text-xs text-fg-2 sm:block">
							{fmtDate(invitation.invitedAt)}
						</span>
						<div class="flex shrink-0 items-center gap-2">
							<form method="POST" action="?/reject" use:enhance={respondSubmit}>
								<input type="hidden" name="invitationId" value={invitation.id} />
								<Button type="submit" size="sm" variant="secondary">Rechazar</Button>
							</form>
							<form method="POST" action="?/accept" use:enhance={respondSubmit}>
								<input type="hidden" name="invitationId" value={invitation.id} />
								<Button type="submit" size="sm">Aceptar</Button>
							</form>
						</div>
					{/snippet}
				</ListRow>
			</li>
		{/each}
	</ul>
{:else}
	<EmptyState
		icon={Mail}
		title="No tienes invitaciones"
		description="Cuando otro artista te invite a ser coautor de una canción, la verás aquí para aceptarla o rechazarla."
	/>
{/if}
