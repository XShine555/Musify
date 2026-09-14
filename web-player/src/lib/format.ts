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
	return hours > 0 ? `${hours} h ${minutes} min` : `${minutes} min`;
}

const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

export function fmtDate(value: string | number | Date): string {
	return dateFormatter.format(new Date(value));
}
