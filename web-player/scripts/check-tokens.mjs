#!/usr/bin/env node

import { readFileSync } from 'node:fs';
import { execSync } from 'node:child_process';

const files = execSync('git ls-files "src/**/*.svelte"', { encoding: 'utf8' })
	.split('\n')
	.filter(Boolean);

const ARBITRARY_WHITELIST = [
	/^text-\[clamp\([^)]+\)\]$/, // tipografía fluida (login, Logo)
	/^leading-\[[\d.]+\]$/, // leading ajustado del display gigante de login
	/^grid-cols-\[auto_1fr\]$/, // upload: portada + campos
	/^max-h-\[85dvh\]$/, // Modal
	/^my-\[\d+d?vh\]$/, // +error.svelte: centrado vertical de página de error
	/^py-\[\d+d?vh\]$/, // Explorar: centrado vertical de "sin resultados"
	/^transition-\[filter\]$/, // Explorar: genre-tile hover
	/^bg-\[image:var\(--mf-[\w-]+\)\]$/
];

const RAW_ROUNDED = /\brounded(-(none|xs|sm|md|lg|xl|2xl|3xl))?(?![\w-])/g;

const PAIRED_HW = /\b(?:([a-z0-9-]+:))?h-([\d.]+)\s+\1w-\2\b/g;
const QUARTER_STEP_SPACING = /\b[a-z-]+-\d+\.(25|75)\b/g;
const HEX_COLOR = /#[0-9a-fA-F]{3,8}\b/g;
const RGBA_LITERAL = /\brgba?\(/g;

let violations = [];

for (const file of files) {
	const content = readFileSync(file, 'utf8');
	const lines = content.split('\n');

	lines.forEach((line, i) => {
		const lineNo = i + 1;

		for (const match of line.matchAll(/[a-zA-Z][a-zA-Z0-9-]*-\[[^\]]+\]/g)) {
			if (!ARBITRARY_WHITELIST.some((re) => re.test(match[0]))) {
				violations.push(`${file}:${lineNo} — arbitrario fuera de la lista blanca: \`${match[0]}\``);
			}
		}

		for (const match of line.matchAll(RAW_ROUNDED)) {
			violations.push(`${file}:${lineNo} — radio de Tailwind sin tokenizar: \`${match[0]}\``);
		}

		for (const match of line.matchAll(PAIRED_HW)) {
			violations.push(`${file}:${lineNo} — usa \`size-${match[2]}\` en vez de \`${match[0]}\``);
		}

		for (const match of line.matchAll(QUARTER_STEP_SPACING)) {
			violations.push(
				`${file}:${lineNo} — espaciado fraccionario en cuartos: \`${match[0]}\` (redondear al entero más cercano)`
			);
		}

		for (const match of line.matchAll(HEX_COLOR)) {
			violations.push(
				`${file}:${lineNo} — color hex literal: \`${match[0]}\` (usa un token --mf-*)`
			);
		}

		for (const match of line.matchAll(RGBA_LITERAL)) {
			violations.push(
				`${file}:${lineNo} — color rgba() literal: \`${match[0]}\` (usa un token --mf-*)`
			);
		}
	});
}

if (violations.length > 0) {
	console.error(`\n${violations.length} violación(es) del sistema de diseño:\n`);
	for (const v of violations) console.error(`  ${v}`);
	console.error('\nVer docs/frontend-refactor-plan.md, sección B.4.\n');
	process.exit(1);
}

console.log('check-tokens: sin violaciones.');
