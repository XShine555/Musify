<script lang="ts">
	import { untrack } from 'svelte';
	import type { ActionResult, SubmitFunction } from '@sveltejs/kit';
	import { enhance } from '$app/forms';
	import { invalidateAll } from '$app/navigation';
	import X from '@lucide/svelte/icons/x';
	import type { CoauthorInvite, LibraryTrack } from '$lib/types';
	import { COAUTHOR_INVITES_MAX, SEARCH_DEBOUNCE_MS, SEARCH_MIN_LENGTH } from '$lib/config';
	import { COAUTHOR_STATUS, inviteCandidates, type UserOption } from '$lib/data/coauthors';
	import Modal from '$lib/components/ui/overlay/Modal.svelte';
	import TagInput from '$lib/components/ui/forms/TagInput.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import Avatar from '$lib/components/ui/media/Avatar.svelte';
	import StatusBadge from '$lib/components/ui/media/StatusBadge.svelte';

	interface Props {
		open: boolean;
		onClose: () => void;
		track: LibraryTrack;
	}

	let { open, onClose, track }: Props = $props();

	const listId = $props.id();

	let invites = $state<CoauthorInvite[]>([]);
	let selected = $state<UserOption[]>([]);
	let search = $state('');
	let results = $state<UserOption[]>([]);
	let error = $state('');
	let sending = $state(false);
	let timer: ReturnType<typeof setTimeout> | undefined;
	let searchToken = 0;

	$effect(() => {
		if (!open) return;
		track.id;
		untrack(() => {
			invites = track.coauthorInvites;
			selected = [];
			search = '';
			results = [];
			error = '';
			sending = false;
		});
	});

	const candidates = $derived(
		inviteCandidates(results, {
			ownerId: track.ownerUserId,
			selectedIds: selected.map((user) => user.id),
			invites
		})
	);
	const full = $derived(selected.length >= COAUTHOR_INVITES_MAX);

	function onSearchInput() {
		clearTimeout(timer);
		const query = search.trim();
		const token = ++searchToken;
		if (query.length < SEARCH_MIN_LENGTH) {
			results = [];
			return;
		}
		timer = setTimeout(async () => {
			try {
				const res = await fetch(`/api/users/search?${new URLSearchParams({ q: query })}`);
				if (!res.ok) throw new Error(String(res.status));
				const found = (await res.json()) as UserOption[];
				if (token === searchToken) results = found;
			} catch {
				if (token === searchToken) {
					results = [];
					error = 'No se pudo buscar usuarios.';
				}
			}
		}, SEARCH_DEBOUNCE_MS);
	}

	function select(user: UserOption) {
		if (full || selected.some((item) => item.id === user.id)) return;
		selected = [...selected, user];
		search = '';
		results = [];
		searchToken++;
	}

	function deselect(id: string) {
		selected = selected.filter((user) => user.id !== id);
	}

	function failureMessage(result: ActionResult, fallback: string): string {
		return result.type === 'failure' && typeof result.data?.message === 'string'
			? result.data.message
			: fallback;
	}

	const inviteSubmit: SubmitFunction = () => {
		sending = true;
		error = '';
		return async ({ result }) => {
			sending = false;
			if (result.type === 'success') {
				invites = (result.data as { invites: CoauthorInvite[] }).invites;
				selected = [];
				await invalidateAll();
			} else {
				error = failureMessage(result, 'No se pudieron enviar las invitaciones.');
			}
		};
	};

	const removeSubmit: SubmitFunction = ({ formData }) => {
		const userId = String(formData.get('userId'));
		const previous = invites;
		invites = invites.filter((invite) => invite.userId !== userId);
		error = '';
		return async ({ result }) => {
			if (result.type === 'success') {
				await invalidateAll();
				return;
			}
			invites = previous;
			error = failureMessage(result, 'No se pudo quitar al coautor.');
		};
	};
</script>

<Modal {open} {onClose} eyebrow={track.title} title="Coautores" maxWidth="max-w-lg">
	<p class="mt-2 text-sm text-fg-2">
		Los usuarios que invites aparecerán como coautores cuando acepten la invitación.
	</p>

	{#if error}
		<Alert tone="danger" class="mt-4">{error}</Alert>
	{/if}

	<section class="mt-5" aria-label="Invitaciones enviadas">
		{#if invites.length > 0}
			<ul class="flex flex-col gap-1">
				{#each invites as invite (invite.userId)}
					<li class="flex items-center gap-3 rounded-control py-1.5 pl-2 hover:bg-hover">
						<Avatar name={invite.name} size="sm" />
						<a
							href="/user/{invite.userId}"
							class="min-w-0 flex-1 truncate text-sm text-fg hover:underline"
						>
							{invite.name}
						</a>
						<StatusBadge
							label={COAUTHOR_STATUS[invite.status].label}
							tone={COAUTHOR_STATUS[invite.status].tone}
						/>
						<form method="POST" action="?/removeCoauthor" use:enhance={removeSubmit}>
							<input type="hidden" name="trackId" value={track.id} />
							<input type="hidden" name="userId" value={invite.userId} />
							<IconButton label="Quitar a {invite.name}" type="submit" size="xs">
								<X class="size-icon-sm" />
							</IconButton>
						</form>
					</li>
				{/each}
			</ul>
		{:else}
			<p class="text-sm text-fg-2">Todavía no has invitado a nadie.</p>
		{/if}
	</section>

	<form method="POST" action="?/inviteCoauthors" use:enhance={inviteSubmit} class="mt-6">
		<input type="hidden" name="trackId" value={track.id} />
		{#each selected as user (user.id)}
			<input type="hidden" name="userIds" value={user.id} />
		{/each}
		<label for="{listId}-search" class="mb-2 block text-sm font-medium text-fg-2">
			Invitar usuarios
		</label>
		<TagInput
			id="{listId}-search"
			label="Buscar usuarios"
			placeholder={full ? `Máximo ${COAUTHOR_INVITES_MAX} usuarios` : 'Buscar por nombre'}
			disabled={full || sending}
			controls="{listId}-results"
			tags={selected.map((user) => ({ id: user.id, label: user.name }))}
			bind:value={search}
			oninput={onSearchInput}
			onadd={() => {
				if (candidates.length > 0) select(candidates[0]);
			}}
			onremove={deselect}
		/>
		<ul id="{listId}-results" aria-label="Resultados" class="mt-2 flex flex-col gap-0.5">
			{#each candidates as user (user.id)}
				<li>
					<button
						type="button"
						onclick={() => select(user)}
						class="flex w-full items-center gap-3 rounded-control p-1.5 text-left text-sm text-fg focus-ring transition-colors hover:bg-hover"
					>
						<Avatar name={user.name} src={user.profilePictureUrl} size="sm" />
						<span class="min-w-0 flex-1 truncate">{user.name}</span>
					</button>
				</li>
			{/each}
		</ul>
		<div class="mt-5 flex justify-end gap-2.5">
			<Button variant="secondary" onclick={onClose}>Cerrar</Button>
			<Button type="submit" disabled={selected.length === 0 || sending}>
				{selected.length > 1 ? `Invitar a ${selected.length} usuarios` : 'Enviar invitación'}
			</Button>
		</div>
	</form>
</Modal>
