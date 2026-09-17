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

export function fmtPlays(count: number | string | undefined): string {
	const value = Math.max(0, Math.floor(Number(count ?? 0)));
	return `${value} ${value === 1 ? 'Reproducción' : 'Reproducciones'}`;
}
