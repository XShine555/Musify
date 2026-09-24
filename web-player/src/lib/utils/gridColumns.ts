export interface GridColumn {
	id: string;
	label?: string;
	width: string;
	mobile?: string;
	align?: 'center';
	mobileLabel?: boolean;
	customHead?: boolean;
	class?: string;
}

export const COL = {
	check: '1.75rem',
	index: '2rem',
	indexMobile: '1.75rem',
	title: 'minmax(11.25rem,1fr)',
	titleMobile: 'minmax(7.5rem,1fr)',
	titleWide: 'minmax(12.5rem,1fr)',
	action: '2.25rem',
	actionMobile: '2rem',
	duration: '5rem',
	durationWide: '6.5rem',
	durationMobile: '3.25rem',
	date: '7.75rem',
	plays: '6.5rem',
	album: 'minmax(6rem,10rem)',
	genres: 'minmax(6rem,12.5rem)',
	genresWide: 'minmax(6rem,13.75rem)',
	status: '7rem',
	statusQueue: '8.75rem',
	statusMobile: '6rem',
	actions: '4.75rem',
	actionsMobile: '4rem',
	size: '5.5rem',
	durationQueue: '4.5rem'
} as const;

export function gridColumns(columns: readonly GridColumn[]): string {
	const desktop = columns.map((column) => column.width).join(' ');
	const mobile = columns.flatMap((column) => (column.mobile ? [column.mobile] : [])).join(' ');
	return `--mf-track-cols:${desktop}; --mf-track-cols-mobile:${mobile}`;
}
