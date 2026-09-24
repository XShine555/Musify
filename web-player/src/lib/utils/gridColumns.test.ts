import { describe, expect, it } from 'vitest';
import { gridColumns } from './gridColumns';

describe('gridColumns', () => {
	it('builds desktop and mobile templates, skipping columns without a mobile width', () => {
		const style = gridColumns([
			{ id: 'title', width: '1fr', mobile: '2fr' },
			{ id: 'album', width: '10rem' },
			{ id: 'action', width: '2rem', mobile: '1rem' }
		]);
		expect(style).toBe('--mf-track-cols:1fr 10rem 2rem; --mf-track-cols-mobile:2fr 1rem');
	});
});
