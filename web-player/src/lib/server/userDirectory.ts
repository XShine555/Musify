export interface DirectoryUser {
	id: string;
	name: string;
	handle: string;
}

const DIRECTORY: DirectoryUser[] = [
	{ id: 'u1', name: 'Alex Rivera', handle: 'alexrivera' },
	{ id: 'u2', name: 'Marina Cordero', handle: 'marinac' },
	{ id: 'u3', name: 'Diego Fuentes', handle: 'dfuentes' },
	{ id: 'u4', name: 'Lucía Marés', handle: 'luciamares' },
	{ id: 'u5', name: 'Noah Bennett', handle: 'noahb' },
	{ id: 'u6', name: 'Sara Ibáñez', handle: 'saraib' },
	{ id: 'u7', name: 'Kairo', handle: 'kairo' },
	{ id: 'u8', name: 'Valentina Ortiz', handle: 'valen' }
];

export function searchUsers(query: string, limit = 12): DirectoryUser[] {
	const term = query.trim().toLowerCase();
	if (!term) return [];
	return DIRECTORY.filter(
		(user) => user.name.toLowerCase().includes(term) || user.handle.toLowerCase().includes(term)
	).slice(0, limit);
}
