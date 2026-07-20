const GREETINGS_BY_HOUR: { until: number; pool: string[] }[] = [
	{
		until: 6,
		pool: ['¿Aún despierto?', 'La noche también tiene banda sonora', 'Un ratito más, con música']
	},
	{
		until: 12,
		pool: ['Buenos días', 'Que empiece bien la mañana', 'Un nuevo día, una nueva lista']
	},
	{ until: 14, pool: ['Buenos días', 'El mediodía sabe mejor con música'] },
	{
		until: 19,
		pool: ['Buenas tardes', 'La tarde pide su banda sonora', 'Un respiro, con algo de música']
	},
	{ until: 23, pool: ['Buenas noches', 'Hora de bajar el ritmo', 'Que la noche te acompañe'] },
	{ until: 24, pool: ['¿Aún despierto?', 'La noche también tiene banda sonora'] }
];

export function pickGreeting(hour: number): string {
	const pool =
		GREETINGS_BY_HOUR.find((slot) => hour < slot.until)?.pool ?? GREETINGS_BY_HOUR[0].pool;
	const greeting = pool[Math.floor(Math.random() * pool.length)];
	return /[.!?]$/.test(greeting) ? greeting : `${greeting}.`;
}
