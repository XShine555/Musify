export function formString(form: FormData, key: string): string {
	return String(form.get(key) ?? '');
}

export function formFile(form: FormData, key: string): File | null {
	const value = form.get(key);
	return value instanceof File && value.size > 0 ? value : null;
}

export function formIds(form: FormData, key: string): string[] {
	return form
		.getAll(key)
		.flatMap((value) => String(value).split(','))
		.filter(Boolean);
}
