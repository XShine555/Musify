interface SpecialDate {
	month: number;
	day: number;
	message: string;
}

const SPECIAL_DATES: SpecialDate[] = [
	{ month: 1, day: 1, message: 'Nuevo año, nueva música.' },
	{ month: 2, day: 14, message: 'Hoy también hay canciones para eso.' },
	{ month: 10, day: 31, message: 'Esta noche suena diferente.' },
	{ month: 12, day: 24, message: 'Nochebuena. Que no pare la música.' },
	{ month: 12, day: 25, message: 'Feliz Navidad. Pon tu banda sonora.' },
	{ month: 12, day: 31, message: 'Último día del año. Haz que suene bien.' }
];

const BIRTHDAY_MESSAGE = 'Hoy toca celebrar. Tú eliges la música.';

const HOUR_SLOTS = [6, 9, 12, 15, 18, 21, 24];

const WEEKLY_SCHEDULE: string[][] = [
	[
		'Domingo de madrugada.',
		'Buenos días. Tómate el domingo con calma.',
		'Domingo. Empieza despacio.',
		'Una mañana tranquila merece buena música.',
		'Domingo por la tarde. Hora de bajar el ritmo.',
		'La semana empieza a despedirse.',
		'Una última canción antes de volver a empezar.'
	],
	[
		'La semana acaba de empezar.',
		'Buenos días. Empezamos otra semana.',
		'Lunes. Dale una banda sonora al día.',
		'Una pausa musical para este lunes.',
		'El lunes todavía no ha terminado.',
		'El lunes ya empieza a quedar atrás.',
		'Sobrevive al lunes con una última canción.'
	],
	[
		'La noche sigue sonando.',
		'Buenos días. Martes en marcha.',
		'La semana ya está cogiendo ritmo.',
		'Martes al mediodía. ¿Qué escuchamos?',
		'Una tarde cualquiera necesita música.',
		'El día empieza a bajar el ritmo.',
		'Un martes más llega a su fin.'
	],
	[
		'Mitad de semana, mitad de noche.',
		'Buenos días. Ya es miércoles.',
		'Ya estamos en el ecuador de la semana.',
		'Miércoles al mediodía. Un poco de música.',
		'La semana ya va cuesta abajo.',
		'Unas canciones para terminar el miércoles.',
		'Mitad de semana superada.'
	],
	[
		'Una noche más antes del fin de semana.',
		'Buenos días. Ya casi estamos.',
		'Jueves. El fin de semana se acerca.',
		'Una pausa musical antes de seguir.',
		'Queda poco para desconectar.',
		'Ya se empieza a notar el viernes.',
		'Una noche más y llega el fin de semana.'
	],
	[
		'La semana ha terminado. La noche continúa.',
		'Buenos días. Es viernes.',
		'Viernes. Hoy suena diferente.',
		'Ya huele a fin de semana.',
		'Últimas horas de la semana.',
		'Se acabó la semana. Dale al play.',
		'Viernes por la noche. Tú eliges la banda sonora.'
	],
	[
		'La noche es joven. Que siga sonando.',
		'Buenos días. Hoy no hay prisa.',
		'Sábado. Empieza el día a tu ritmo.',
		'Mediodía de sábado. ¿Qué ponemos?',
		'Una tarde de sábado pide música.',
		'El sábado todavía tiene mucho por delante.',
		'Esta noche puede sonar cualquier cosa.'
	]
];

export interface Birthday {
	month: number;
	day: number;
}

export function pickGreeting(date: Date, birthday?: Birthday | null): string {
	const month = date.getMonth() + 1;
	const day = date.getDate();

	if (birthday && birthday.month === month && birthday.day === day) return BIRTHDAY_MESSAGE;

	const special = SPECIAL_DATES.find((d) => d.month === month && d.day === day);
	if (special) return special.message;

	const hour = date.getHours();
	const slot = HOUR_SLOTS.findIndex((until) => hour < until);
	return WEEKLY_SCHEDULE[date.getDay()][slot === -1 ? 0 : slot];
}
