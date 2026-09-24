import { fail, type RequestEvent } from '@sveltejs/kit';
import { apiFor, type ApiClient } from '$lib/server/api';

export interface ActionContext {
	api: ApiClient;
	form: FormData;
	params: Record<string, string>;
	locals: App.Locals;
	url: URL;
}

export function authedAction<R>(
	handler: (context: ActionContext) => R | Promise<R>,
	unauthorizedMessage = 'Inicia sesión.'
) {
	return async (event: RequestEvent) => {
		if (!event.locals.accessToken) return fail(401, { message: unauthorizedMessage });
		const form = await event.request.formData();
		return handler({
			api: apiFor(event),
			form,
			params: event.params as Record<string, string>,
			locals: event.locals,
			url: event.url
		});
	};
}
