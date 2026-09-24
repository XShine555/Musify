import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const fallback: RequestHandler = () => json({ message: 'No encontrado.' }, { status: 404 });
