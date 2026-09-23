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

const WEEKLY_SCHEDULE: [string, string][][] = [
	[
		['Domingo de madrugada.', 'Madrugada de domingo. Todo va más lento.'],
		['Buenos días. Tómate el domingo con calma.', 'Buenos días. Hoy nadie tiene prisa.'],
		['Domingo. Empieza despacio.', 'Un domingo así pide algo tranquilo.'],
		['Una mañana tranquila merece buena música.', 'Domingo al mediodía. Pon algo que abrace.'],
		[
			'Domingo por la tarde. Hora de bajar el ritmo.',
			'La tarde del domingo se disfruta con calma.'
		],
		['La semana empieza a despedirse.', 'El domingo se va apagando. Deja que suene.'],
		['Una última canción antes de volver a empezar.', 'Cierra el domingo con algo que te guste.']
	],
	[
		['La semana acaba de empezar.', 'Ya es lunes, aunque aún sea de noche.'],
		['Buenos días. Empezamos otra semana.', 'Lunes por la mañana. Arranca con buen pie.'],
		['Lunes. Dale una banda sonora al día.', 'El lunes se lleva mejor con música.'],
		['Una pausa musical para este lunes.', 'Mediodía de lunes. Te has ganado un respiro.'],
		['El lunes todavía no ha terminado.', 'Media tarde de lunes. Aguanta un poco más.'],
		['El lunes ya empieza a quedar atrás.', 'Casi lo has conseguido. Un lunes menos.'],
		['Sobrevive al lunes con una última canción.', 'Lunes superado. Ahora, lo que tú quieras.']
	],
	[
		['La noche sigue sonando.', 'Martes de madrugada. La música no duerme.'],
		['Buenos días. Martes en marcha.', 'Martes. Ya estás más despierto que ayer.'],
		['La semana ya está cogiendo ritmo.', 'El martes ya tiene su propio compás.'],
		['Martes al mediodía. ¿Qué escuchamos?', 'A mitad del martes. Una canción de por medio.'],
		['Una tarde cualquiera necesita música.', 'Tarde de martes. Ponle algo con ritmo.'],
		['El día empieza a bajar el ritmo.', 'Cae la tarde. Es buen momento para poner algo.'],
		['Un martes más llega a su fin.', 'El martes se acaba. Ciérralo con música.']
	],
	[
		['Mitad de semana, mitad de noche.', 'Miércoles de madrugada. Justo en el centro.'],
		['Buenos días. Ya es miércoles.', 'Miércoles. La semana ya está de tu lado.'],
		['Ya estamos en el ecuador de la semana.', 'Ecuador de la semana. Lo peor ya pasó.'],
		['Miércoles al mediodía. Un poco de música.', 'Un descanso en pleno miércoles.'],
		['La semana ya va cuesta abajo.', 'Miércoles por la tarde. Todo empieza a rodar.'],
		['Unas canciones para terminar el miércoles.', 'El miércoles se despide. Ponle banda sonora.'],
		['Mitad de semana superada.', 'Ya solo queda bajar la cuesta.']
	],
	[
		['Una noche más antes del fin de semana.', 'Jueves de madrugada. Queda menos.'],
		['Buenos días. Ya casi estamos.', 'Jueves. Hoy se huele el viernes.'],
		['Jueves. El fin de semana se acerca.', 'Un jueves con buena música se pasa volando.'],
		['Una pausa musical antes de seguir.', 'Mediodía de jueves. Toca recargar pilas.'],
		['Queda poco para desconectar.', 'Tarde de jueves. El fin de semana asoma.'],
		['Ya se empieza a notar el viernes.', 'El jueves ya está de salida.'],
		['Una noche más y llega el fin de semana.', 'Última noche de la semana laboral. Que suene.']
	],
	[
		['La semana ha terminado. La noche continúa.', 'Viernes de madrugada. Aún queda noche.'],
		['Buenos días. Es viernes.', 'Viernes. Hoy ya se nota el ambiente.'],
		['Viernes. Hoy suena diferente.', 'Es viernes. Sube el volumen.'],
		['Ya huele a fin de semana.', 'Mediodía de viernes. Casi libre.'],
		['Últimas horas de la semana.', 'Tarde de viernes. La semana ya es historia.'],
		['Se acabó la semana. Dale al play.', 'Empieza el fin de semana. Que empiece con música.'],
		['Viernes por la noche. Tú eliges la banda sonora.', 'Noche de viernes. Que no falte música.']
	],
	[
		['La noche es joven. Que siga sonando.', 'Sábado de madrugada. La fiesta no acaba.'],
		['Buenos días. Hoy no hay prisa.', 'Sábado por la mañana. Sin alarma y con música.'],
		['Sábado. Empieza el día a tu ritmo.', 'Hoy manda el sábado. Tú pones el ritmo.'],
		['Mediodía de sábado. ¿Qué ponemos?', 'Sábado al mediodía. Hora de elegir banda sonora.'],
		['Una tarde de sábado pide música.', 'Tarde de sábado. Sin planes también se disfruta.'],
		['El sábado todavía tiene mucho por delante.', 'Cae el sábado y aún queda mucho por sonar.'],
		['Esta noche puede sonar cualquier cosa.', 'Noche de sábado. Todo vale, elige tú.']
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
	const variant = Math.floor(date.getTime() / 86_400_000) % 2;
	return WEEKLY_SCHEDULE[date.getDay()][slot === -1 ? 0 : slot][variant];
}
