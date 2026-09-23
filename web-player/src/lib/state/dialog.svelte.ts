export interface DialogAction {
	label: string;
	variant?: 'primary' | 'secondary' | 'accent' | 'danger';
	onclick?: () => void;
}

export interface DialogOptions {
	title: string;
	description?: string;
	tone?: 'neutral' | 'danger';
	actions?: DialogAction[];
}

class DialogStore {
	current = $state<DialogOptions | null>(null);

	show(options: DialogOptions) {
		this.current = options;
	}

	error(title: string, description?: string, actions?: DialogAction[]) {
		this.show({ title, description, tone: 'danger', actions });
	}

	close() {
		this.current = null;
	}

	run(action: DialogAction) {
		this.close();
		action.onclick?.();
	}
}

export const dialog = new DialogStore();
