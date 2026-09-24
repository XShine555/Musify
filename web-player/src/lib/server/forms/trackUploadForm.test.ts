import { describe, expect, it } from 'vitest';
import { LIMITS } from '$lib/validation';
import { parseTrackUploadForm } from './trackUploadForm';

function form(fields: Record<string, string | string[] | File>): FormData {
	const data = new FormData();
	for (const [key, value] of Object.entries(fields)) {
		if (Array.isArray(value)) value.forEach((item) => data.append(key, item));
		else data.set(key, value);
	}
	return data;
}

const audio = new File(['a'], 'a.mp3');
const cover = new File(['c'], 'c.png');

describe('parseTrackUploadForm', () => {
	it('parses a valid form and deduplicates tags', () => {
		const parsed = parseTrackUploadForm(
			form({ title: ' Song ', audio, cover, tags: ['rock', 'rock', 'pop'], isExplicit: 'on' })
		);
		expect(parsed).toMatchObject({
			body: { title: 'Song', tags: ['rock', 'pop'], isExplicit: true }
		});
	});
	it('validates each field', () => {
		const ok = { title: 'Song', audio, cover, tags: ['rock'] };
		expect(parseTrackUploadForm(form({ ...ok, title: '' }))).toHaveProperty('failMessage');
		expect(
			parseTrackUploadForm(form({ ...ok, title: 'x'.repeat(LIMITS.trackTitle + 1) }))
		).toHaveProperty('failMessage');
		expect(parseTrackUploadForm(form({ ...ok, audio: '' }))).toHaveProperty('failMessage');
		expect(parseTrackUploadForm(form({ ...ok, cover: '' }))).toHaveProperty('failMessage');
		expect(parseTrackUploadForm(form({ ...ok, tags: [] }))).toHaveProperty('failMessage');
	});
});
