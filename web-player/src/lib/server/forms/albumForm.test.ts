import { describe, expect, it } from 'vitest';
import { ALBUM_EARLIEST_YEAR } from '$lib/validation';
import { parseAlbumForm } from './albumForm';

function form(fields: Record<string, string>): FormData {
	const data = new FormData();
	for (const [key, value] of Object.entries(fields)) data.set(key, value);
	return data;
}

describe('parseAlbumForm', () => {
	it('parses a valid form and trims values', () => {
		expect(
			parseAlbumForm(form({ title: ' Title ', description: ' d ', releaseYear: '2020' }))
		).toEqual({ body: { title: 'Title', description: 'd', releaseYear: 2020 } });
	});
	it('turns empty optional fields into null', () => {
		expect(parseAlbumForm(form({ title: 'T' }))).toEqual({
			body: { title: 'T', description: null, releaseYear: null }
		});
	});
	it('rejects empty or too long titles', () => {
		expect(parseAlbumForm(form({ title: '  ' }))).toHaveProperty('failMessage');
		expect(parseAlbumForm(form({ title: 'x'.repeat(201) }))).toHaveProperty('failMessage');
	});
	it('rejects too long descriptions', () => {
		expect(parseAlbumForm(form({ title: 'T', description: 'x'.repeat(257) }))).toHaveProperty(
			'failMessage'
		);
	});
	it('rejects invalid years', () => {
		for (const releaseYear of ['abc', '2020.5', String(ALBUM_EARLIEST_YEAR - 1), '9999']) {
			expect(parseAlbumForm(form({ title: 'T', releaseYear }))).toHaveProperty('failMessage');
		}
	});
});
