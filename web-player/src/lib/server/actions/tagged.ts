import { fail, isActionFailure } from '@sveltejs/kit';
import { authedAction, type ActionContext } from '$lib/server/actions/authedAction';

export function tagged<T extends object>(
	action: string,
	handler: (context: ActionContext) => Promise<T>
) {
	return authedAction(async (context) => {
		const result = await handler(context);
		return isActionFailure(result)
			? fail(result.status, { ...(result.data as Record<string, unknown> | undefined), action })
			: { ...result, action };
	});
}
