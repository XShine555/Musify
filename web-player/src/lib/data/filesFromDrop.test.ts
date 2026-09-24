import { describe, expect, it } from 'vitest';
import { filesFromDrop } from './uploadQueue';

function fileEntry(name: string) {
	const file = new File(['x'], name);
	return {
		isFile: true,
		isDirectory: false,
		file: (resolve: (file: File) => void) => resolve(file)
	};
}

function directoryEntry(children: unknown[]) {
	let served = false;
	return {
		isFile: false,
		isDirectory: true,
		createReader: () => ({
			readEntries: (resolve: (entries: unknown[]) => void) => {
				resolve(served ? [] : children);
				served = true;
			}
		})
	};
}

function transfer(entries: unknown[], files: File[] = []) {
	return {
		items: entries.map((entry) => ({ webkitGetAsEntry: () => entry })),
		files
	} as unknown as DataTransfer;
}

describe('filesFromDrop', () => {
	it('collects files from nested folders', async () => {
		const dropped = transfer([
			fileEntry('a.mp3'),
			directoryEntry([fileEntry('b.mp3'), directoryEntry([fileEntry('c.mp3')])])
		]);
		const files = await filesFromDrop(dropped);
		expect(files.map((file) => file.name).sort()).toEqual(['a.mp3', 'b.mp3', 'c.mp3']);
	});

	it('falls back to the plain file list without entries', async () => {
		const plain = new File(['x'], 'd.mp3');
		const dropped = {
			items: [{ webkitGetAsEntry: () => null }],
			files: [plain]
		} as unknown as DataTransfer;
		expect(await filesFromDrop(dropped)).toEqual([plain]);
	});
});
