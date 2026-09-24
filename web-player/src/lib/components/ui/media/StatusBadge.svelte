<script lang="ts">
	interface Props {
		label: string;
		tone?: 'success' | 'neutral' | 'danger';
		pulse?: boolean;
		class?: string;
	}

	let { label, tone = 'neutral', pulse = false, class: klass = '' }: Props = $props();

	const TONE = {
		success: 'bg-accent-tint text-accent-soft',
		neutral: 'bg-btn text-fg-2',
		danger: 'bg-danger-tint text-danger-fg'
	};
</script>

<span
	class="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium whitespace-nowrap {pulse
		? 'animate-pulse'
		: ''} {TONE[tone]} {klass}">{label}</span
>
