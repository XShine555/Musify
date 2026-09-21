export function createToggle(initial = false) {
	let open = $state(initial);

	return {
		get open() {
			return open;
		},
		show() {
			open = true;
		},
		close() {
			open = false;
		},
		toggle() {
			open = !open;
		}
	};
}
