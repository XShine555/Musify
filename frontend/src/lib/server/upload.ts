import http from 'node:http';
import https from 'node:https';
import { env } from '$env/dynamic/private';

const CONNECT_HOST = env.S3_UPLOAD_CONNECT_HOST || '127.0.0.1';

export const IMAGE_TYPES: Record<string, string> = {
	jpg: 'image/jpeg',
	jpeg: 'image/jpeg',
	png: 'image/png',
	webp: 'image/webp'
};

export const AUDIO_TYPES: Record<string, string> = {
	mp3: 'audio/mpeg',
	m4a: 'audio/mp4',
	aac: 'audio/aac',
	flac: 'audio/flac',
	wav: 'audio/wav',
	ogg: 'audio/ogg',
	opus: 'audio/opus'
};

export function extOf(name: string): string {
	const dot = name.lastIndexOf('.');
	return dot >= 0 ? name.slice(dot + 1).toLowerCase() : '';
}

export function contentTypeOf(file: File, table: Record<string, string> = IMAGE_TYPES): string {
	if (file.type) return file.type;
	return table[extOf(file.name)] ?? 'application/octet-stream';
}

export async function putPresigned(
	presignedUrl: string,
	body: Buffer,
	contentType: string
): Promise<void> {
	const url = new URL(presignedUrl);
	const signed = new Set(
		(url.searchParams.get('X-Amz-SignedHeaders') ?? 'host').toLowerCase().split(';')
	);

	const headers: Record<string, string | number> = {
		Host: url.host,
		'Content-Length': body.byteLength
	};
	if (signed.has('content-type')) headers['Content-Type'] = contentType;
	if (signed.has('if-none-match')) headers['If-None-Match'] = '*';

	const secure = url.protocol === 'https:';
	const transport = secure ? https : http;
	const port = url.port || (secure ? 443 : 80);

	await new Promise<void>((resolve, reject) => {
		const request = transport.request(
			{
				host: CONNECT_HOST,
				port,
				method: 'PUT',
				path: url.pathname + url.search,
				headers
			},
			(response) => {
				let raw = '';
				response.on('data', (chunk) => (raw += chunk));
				response.on('end', () => {
					const status = response.statusCode ?? 0;
					if (status >= 200 && status < 300) resolve();
					else reject(new Error(`S3 PUT ${status}: ${raw.slice(0, 200)}`));
				});
			}
		);
		request.on('error', reject);
		request.write(body);
		request.end();
	});
}

export type UploadImageResult = { intentId: string } | { failMessage: string; detail?: string };

export async function uploadPresignedImage(
	reserve: (args: {
		fileType: string;
		contentType: string;
		expectedSizeBytes: number;
	}) => Promise<{
		data?: { uploadUrl: string; contentType: string; intentId: string } | null;
		error?: unknown;
	}>,
	file: File
): Promise<UploadImageResult> {
	const { data: reserved, error: reserveError } = await reserve({
		fileType: extOf(file.name) || 'jpg',
		contentType: contentTypeOf(file),
		expectedSizeBytes: file.size
	});

	if (reserveError || !reserved) {
		return { failMessage: 'No se pudo reservar la subida de la portada.' };
	}

	try {
		const buffer = await file.arrayBuffer();
		await putPresigned(reserved.uploadUrl, Buffer.from(buffer), reserved.contentType);
	} catch (err) {
		return {
			failMessage: 'Falló la subida de la portada al almacenamiento.',
			detail: err instanceof Error ? err.message : undefined
		};
	}

	return { intentId: reserved.intentId };
}
