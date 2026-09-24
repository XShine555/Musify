import { describe, expect, it } from 'vitest';
import {
	canPublish,
	filterCounts,
	filterQuery,
	parseTrackFilter,
	trackState
} from './libraryTracks';

describe('trackState', () => {
	it('never calls a processed track published unless it is', () => {
		expect(trackState({ status: 'Completed', published: false }).label).toBe('Sin publicar');
		expect(trackState({ status: 'Completed', published: true }).label).toBe('Publicada');
	});
	it('pulses while processing', () => {
		expect(trackState({ status: 'Processing', published: false })).toMatchObject({
			label: 'Procesando',
			pulse: true
		});
		expect(trackState({ status: 'Failed', published: false }).tone).toBe('danger');
	});
});

describe('canPublish', () => {
	it('only allows processed tracks', () => {
		expect(canPublish({ status: 'Completed' })).toBe(true);
		expect(canPublish({ status: 'Pending' })).toBe(false);
	});
});

describe('filters', () => {
	it('falls back to "all" for unknown values', () => {
		expect(parseTrackFilter('nope')).toBe('all');
		expect(parseTrackFilter(null)).toBe('all');
		expect(parseTrackFilter('failed')).toBe('failed');
	});
	it('maps a filter to an API query', () => {
		expect(filterQuery('processing')).toEqual({ status: ['Pending', 'Processing'] });
		expect(filterQuery('unpublished')).toEqual({ status: ['Completed'], published: false });
		expect(filterQuery('all')).toEqual({});
	});
	it('derives the chip counts', () => {
		expect(
			filterCounts({ total: 10, processing: 2, completed: 6, failed: 2, published: 4 })
		).toEqual({ all: 10, published: 4, unpublished: 2, processing: 2, failed: 2 });
	});
});
