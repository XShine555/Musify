import http from 'node:http';
import https from 'node:https';
import { env } from '$env/dynamic/private';

const CONNECT_HOST = env.S3_UPLOAD_CONNECT_HOST || '127.0.0.1';

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
