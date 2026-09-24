import { describe, expect, it } from 'vitest';
import { apiErrorDetail } from './api';

describe('apiErrorDetail', () => {
	it('joins the validation messages from errors', () => {
		expect(
			apiErrorDetail({ errors: { Name: ['Too short.', 'Required.'], Genre: ['Invalid.'] } })
		).toBe('Too short. Required. Invalid.');
	});

	it('flattens arrays and ignores non-string entries', () => {
		expect(apiErrorDetail({ errors: { A: ['one', 2, null], B: 'two' } })).toBe('one two');
	});

	it('falls back to detail when errors has no text', () => {
		expect(apiErrorDetail({ errors: {}, detail: 'Bad request.' })).toBe('Bad request.');
	});

	it('falls back to title after detail', () => {
		expect(apiErrorDetail({ detail: '', title: 'Conflict' })).toBe('Conflict');
		expect(apiErrorDetail({ title: 'Conflict' })).toBe('Conflict');
	});

	it('returns undefined for non-objects and empty objects', () => {
		expect(apiErrorDetail(undefined)).toBeUndefined();
		expect(apiErrorDetail(null)).toBeUndefined();
		expect(apiErrorDetail('boom')).toBeUndefined();
		expect(apiErrorDetail({})).toBeUndefined();
		expect(apiErrorDetail({ detail: 3, title: '' })).toBeUndefined();
	});
});
