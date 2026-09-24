import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ListenTracker } from './listenTracker';

let fetchMock: ReturnType<typeof vi.fn>;

function sentSeconds(call: number): number {
	const init = fetchMock.mock.calls[call][1] as RequestInit;
	return JSON.parse(init.body as string).playedSeconds;
}

beforeEach(() => {
	vi.useFakeTimers();
	fetchMock = vi.fn().mockResolvedValue({ ok: true });
	vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
	vi.useRealTimers();
	vi.unstubAllGlobals();
});

function startedTracker(id = 'l1') {
	const tracker = new ListenTracker();
	tracker.begin(id);
	tracker.tick(0);
	return tracker;
}

describe('ListenTracker', () => {
	it('sums small forward advances', () => {
		const tracker = startedTracker();
		tracker.tick(1);
		tracker.tick(2);
		tracker.flush();
		expect(fetchMock).toHaveBeenCalledTimes(1);
		expect(sentSeconds(0)).toBe(2);
	});

	it('ignores seeks forward and backwards', () => {
		const tracker = startedTracker();
		tracker.tick(1);
		tracker.tick(60);
		tracker.tick(10);
		tracker.tick(11);
		tracker.flush();
		expect(sentSeconds(0)).toBe(2);
	});

	it('does not count the tick after interrupt()', () => {
		const tracker = startedTracker();
		tracker.tick(1);
		tracker.interrupt();
		tracker.tick(2);
		tracker.tick(3);
		tracker.flush();
		expect(sentSeconds(0)).toBe(2);
	});

	it('sends after 30 seconds of wall time', () => {
		const tracker = startedTracker();
		tracker.tick(1);
		expect(fetchMock).not.toHaveBeenCalled();
		vi.advanceTimersByTime(30_000);
		tracker.tick(2);
		expect(fetchMock).toHaveBeenCalledTimes(1);
		expect(sentSeconds(0)).toBe(2);
	});

	it('does not resend without new progress', () => {
		const tracker = startedTracker();
		tracker.tick(1);
		tracker.flush();
		tracker.flush();
		expect(fetchMock).toHaveBeenCalledTimes(1);
		tracker.tick(2);
		tracker.flush();
		expect(fetchMock).toHaveBeenCalledTimes(2);
		expect(sentSeconds(1)).toBe(2);
	});

	it('sends nothing when there is no listen id', () => {
		const tracker = new ListenTracker();
		tracker.begin(null);
		tracker.tick(0);
		tracker.tick(1);
		tracker.flush();
		expect(fetchMock).not.toHaveBeenCalled();
	});

	it('resends the total after a failed request', async () => {
		fetchMock.mockRejectedValueOnce(new Error('offline'));
		const tracker = startedTracker();
		tracker.tick(1);
		tracker.flush();
		await vi.runAllTimersAsync();
		tracker.flush();
		expect(fetchMock).toHaveBeenCalledTimes(2);
		expect(sentSeconds(1)).toBe(1);
	});

	it('flushes the previous listen when a new one begins', () => {
		const tracker = startedTracker('old');
		tracker.tick(1);
		tracker.begin('new');
		expect(fetchMock).toHaveBeenCalledTimes(1);
		expect(fetchMock.mock.calls[0][0]).toBe('/api/listens/old/progress');
		tracker.flush();
		expect(fetchMock).toHaveBeenCalledTimes(1);
	});
});
