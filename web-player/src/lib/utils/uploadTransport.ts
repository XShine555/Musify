import type {
	CompleteRequest,
	CompleteResponse,
	PresignRequest,
	PresignResponse,
	PresignTarget,
	StatusResponse
} from '$lib/data/uploadProtocol';

export interface UploadTransport {
	presign(request: PresignRequest, signal: AbortSignal): Promise<PresignResponse>;
	put(
		target: PresignTarget,
		file: Blob,
		onProgress: (fraction: number) => void,
		signal: AbortSignal
	): Promise<void>;
	complete(request: CompleteRequest, signal: AbortSignal): Promise<CompleteResponse>;
	status(ids: string[]): Promise<StatusResponse>;
}

export class TransportError extends Error {}

export function signedHeaders(uploadUrl: string): Set<string> {
	const raw = new URL(uploadUrl).searchParams.get('X-Amz-SignedHeaders') ?? 'host';
	return new Set(raw.toLowerCase().split(';'));
}

export function putHeaders(target: PresignTarget): Record<string, string> {
	const signed = signedHeaders(target.uploadUrl);
	const headers: Record<string, string> = {};
	if (signed.has('content-type')) headers['Content-Type'] = target.contentType;
	if (signed.has('if-none-match')) headers['If-None-Match'] = '*';
	return headers;
}

async function postJson<T>(url: string, body: unknown, signal: AbortSignal): Promise<T> {
	const response = await fetch(url, {
		method: 'POST',
		headers: { 'content-type': 'application/json' },
		body: JSON.stringify(body),
		signal
	});
	const data = await response.json().catch(() => null);
	if (!response.ok) {
		throw new TransportError(data?.message ?? 'La solicitud falló.');
	}
	return data as T;
}

function putWithProgress(
	target: PresignTarget,
	file: Blob,
	onProgress: (fraction: number) => void,
	signal: AbortSignal
): Promise<void> {
	return new Promise((resolve, reject) => {
		if (signal.aborted) {
			reject(new DOMException('Aborted', 'AbortError'));
			return;
		}
		const xhr = new XMLHttpRequest();
		xhr.open('PUT', target.uploadUrl);
		for (const [name, value] of Object.entries(putHeaders(target))) {
			xhr.setRequestHeader(name, value);
		}
		xhr.upload.onprogress = (event) => {
			if (event.lengthComputable && event.total > 0) onProgress(event.loaded / event.total);
		};
		xhr.onload = () => {
			if (xhr.status >= 200 && xhr.status < 300) resolve();
			else reject(new TransportError(`El almacenamiento rechazó el archivo (${xhr.status}).`));
		};
		xhr.onerror = () => reject(new TransportError('No se pudo conectar con el almacenamiento.'));
		xhr.onabort = () => reject(new DOMException('Aborted', 'AbortError'));
		signal.addEventListener('abort', () => xhr.abort(), { once: true });
		xhr.send(file);
	});
}

export const httpUploadTransport: UploadTransport = {
	presign: (request, signal) => postJson<PresignResponse>('/api/uploads', request, signal),
	put: putWithProgress,
	complete: (request, signal) =>
		postJson<CompleteResponse>('/api/uploads/complete', request, signal),
	async status(ids) {
		const response = await fetch(`/api/uploads/status?ids=${encodeURIComponent(ids.join(','))}`);
		if (!response.ok) throw new TransportError('No se pudo consultar el estado.');
		return (await response.json()) as StatusResponse;
	}
};
