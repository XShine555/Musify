import { describe, expect, it } from 'vitest';
import { LIMITS } from '$lib/validation';
import { parsePlaylistForm } from './playlistForm';

function form(fields: Record<string, string>): FormData {
	const data = new FormData();
	for (const [key, value] of Object.entries(fields)) data.set(key, value);
	return data;
}

describe('parsePlaylistForm', () => {
	it('parses a valid form', () => {
		expect(
			parsePlaylistForm(form({ name: ' Mix ', description: ' d ', visibility: 'public' }))
		).toEqual({ body: { name: 'Mix', description: 'd', visibility: 'Public', cover: null } });
	});
	it('defaults to private', () => {
		const parsed = parsePlaylistForm(form({ name: 'Mix' }));
		expect(parsed).toMatchObject({ body: { visibility: 'Private', description: '' } });
	});
	it('rejects empty or too long names keeping the typed values', () => {
		expect(parsePlaylistForm(form({ name: ' ', description: 'd' }))).toMatchObject({
			failMessage: expect.any(String),
			description: 'd'
		});
		expect(parsePlaylistForm(form({ name: 'x'.repeat(LIMITS.playlistName + 1) }))).toHaveProperty(
			'failMessage'
		);
	});
	it('rejects too long descriptions', () => {
		const description = 'x'.repeat(LIMITS.playlistDescription + 1);
		expect(parsePlaylistForm(form({ name: 'Mix', description }))).toHaveProperty('failMessage');
	});
});
