export function fmtTime(seconds: number): string {
	const total = Math.max(0, Math.floor(seconds || 0));
	const minutes = Math.floor(total / 60);
	const rest = total % 60;
	return `${minutes}:${rest < 10 ? '0' : ''}${rest}`;
}

const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

export function fmtDate(value: string | number | Date): string {
	return dateFormatter.format(new Date(value));
}
