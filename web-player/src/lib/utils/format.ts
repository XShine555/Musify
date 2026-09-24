export function fmtTime(seconds: number): string {
	const total = Math.max(0, Math.floor(seconds || 0));
	const minutes = Math.floor(total / 60);
	const rest = total % 60;
	return `${minutes}:${rest < 10 ? '0' : ''}${rest}`;
}

export function fmtDurationLong(seconds: number): string {
	const total = Math.max(0, Math.floor(seconds || 0));
	const hours = Math.floor(total / 3600);
	const minutes = Math.floor((total % 3600) / 60);
	return hours > 0 ? `${hours} H ${minutes} Min` : `${minutes} Min`;
}

const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

export function fmtDate(value: string | number | Date): string {
	return dateFormatter.format(new Date(value));
}

export function plural(n: number, one: string, many: string): string {
	return `${n} ${n === 1 ? one : many}`;
}

export function fmtPlays(count: number): string {
	const value = Math.max(0, Math.floor(count));
	return plural(value, 'reproducción', 'reproducciones');
}

export function albumMeta(releaseYear?: number | null, trackCount?: number): string {
	return [
		releaseYear ? String(releaseYear) : undefined,
		trackCount === undefined ? undefined : plural(trackCount, 'canción', 'canciones')
	]
		.filter((part): part is string => !!part)
		.join(' · ');
}

export function formatSize(bytes: number): string {
	if (bytes === 0) return '';
	const mb = bytes / (1024 * 1024);
	return mb >= 1 ? `${mb.toFixed(1)} MB` : `${Math.round(bytes / 1024)} KB`;
}

export function playlistMeta(trackCount: number): string {
	return plural(trackCount, 'canción', 'canciones');
}
