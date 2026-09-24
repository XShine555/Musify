import { describe, expect, it } from 'vitest';
import { pickGreeting } from './greeting';

const BIRTHDAY = 'Hoy toca celebrar. Tú eliges la música.';

describe('pickGreeting', () => {
	it('uses the birthday message over special dates', () => {
		const date = new Date(2026, 11, 25, 10);
		expect(pickGreeting(date, { month: 12, day: 25 })).toBe(BIRTHDAY);
	});
	it('uses the special date message', () => {
		expect(pickGreeting(new Date(2026, 11, 25, 10))).toBe('Feliz Navidad. Pon tu banda sonora.');
	});
	it('returns a non-empty schedule message at every hour', () => {
		for (let hour = 0; hour < 24; hour++) {
			expect(pickGreeting(new Date(2026, 2, 10, hour)).length).toBeGreaterThan(0);
		}
	});
	it('ignores a birthday on another day', () => {
		expect(pickGreeting(new Date(2026, 2, 10, 10), { month: 1, day: 2 })).not.toBe(BIRTHDAY);
	});
});
