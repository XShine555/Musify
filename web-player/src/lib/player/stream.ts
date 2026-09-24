async function readErrorMessage(res: Response): Promise<string> {
	try {
		const body = (await res.json()) as { message?: string };
		return body?.message || `Error ${res.status}`;
	} catch {
		return `Error ${res.status}`;
	}
}

function withTicket(url: string, ticket: string): string {
	if (!ticket) return url;
	const separator = url.includes('?') ? '&' : '?';
	return `${url}${separator}t=${encodeURIComponent(ticket)}`;
}

export async function resolveStream(id: string): Promise<{ src: string; listenId: string | null }> {
	const res = await fetch(`/api/tracks/${id}/stream`);
	if (!res.ok) throw new Error(await readErrorMessage(res));
	const { manifestUrl, ticket, listenId } = (await res.json()) as {
		manifestUrl: string;
		ticket: string;
		listenId?: string | null;
	};
	return { src: withTicket(manifestUrl, ticket), listenId: listenId ?? null };
}
