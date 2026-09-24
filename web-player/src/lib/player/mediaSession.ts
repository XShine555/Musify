import { browser } from '$app/environment';
import type { Track } from '$lib/types';
import { trackCover } from '$lib/utils/hrefs';

interface MediaSessionHandlers {
	play: () => void;
	pause: () => void;
	previous: () => void;
	next: () => void;
}

const supported = () => browser && 'mediaSession' in navigator;

export const mediaSession = {
	setup(handlers: MediaSessionHandlers) {
		if (!supported()) return;
		navigator.mediaSession.setActionHandler('play', handlers.play);
		navigator.mediaSession.setActionHandler('pause', handlers.pause);
		navigator.mediaSession.setActionHandler('previoustrack', handlers.previous);
		navigator.mediaSession.setActionHandler('nexttrack', handlers.next);
	},

	setPlaying(playing: boolean) {
		if (!supported()) return;
		navigator.mediaSession.playbackState = playing ? 'playing' : 'paused';
	},

	setTrack(track: Track) {
		if (!supported()) return;
		navigator.mediaSession.metadata = new MediaMetadata({
			title: track.title,
			artist: track.artist ?? '',
			artwork: [{ src: trackCover(track.id, 'small'), sizes: '256x256' }]
		});
	}
};
