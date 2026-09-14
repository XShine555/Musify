let open = $state(false);

export const createPlaylistModal = {
	get open() {
		return open;
	},
	show() {
		open = true;
	},
	close() {
		open = false;
	}
};
