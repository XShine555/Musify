let open = $state(false);

export const queuePanel = {
	get open() {
		return open;
	},
	toggle() {
		open = !open;
	},
	close() {
		open = false;
	},
	show() {
		open = true;
	}
};
