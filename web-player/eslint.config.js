import js from '@eslint/js';
import svelte from 'eslint-plugin-svelte';
import prettier from 'eslint-config-prettier';
import globals from 'globals';
import ts from 'typescript-eslint';

export default ts.config(
	js.configs.recommended,
	...ts.configs.recommended,
	...svelte.configs.recommended,
	prettier,
	...svelte.configs.prettier,
	{
		languageOptions: {
			globals: { ...globals.browser, ...globals.node }
		},
		rules: {
			'@typescript-eslint/no-unused-vars': [
				'error',
				{ argsIgnorePattern: '^_', varsIgnorePattern: '^_' }
			],
			'@typescript-eslint/no-non-null-assertion': 'error',
			'no-restricted-imports': [
				'error',
				{
					patterns: [
						{
							group: ['../*'],
							message: 'Use $lib/... instead of relative parent imports.'
						}
					]
				}
			],
			'svelte/no-unused-props': 'error',
			'svelte/no-navigation-without-resolve': 'off',
			'svelte/prefer-svelte-reactivity': 'off'
		}
	},
	{
		files: ['**/*.svelte', '**/*.svelte.ts'],
		languageOptions: {
			parserOptions: {
				extraFileExtensions: ['.svelte'],
				parser: ts.parser
			}
		},
		rules: {
			'@typescript-eslint/no-unused-expressions': 'off'
		}
	},
	{
		files: ['src/lib/components/ui/**'],
		rules: {
			'no-restricted-imports': [
				'error',
				{
					patterns: [
						{
							group: ['../*'],
							message: 'Use $lib/... instead of relative parent imports.'
						},
						{
							group: [
								'$lib/player/*',
								'$lib/data/*',
								'$lib/server/*',
								'$lib/components/music/*',
								'$lib/components/shell/*',
								'$lib/components/player/*'
							],
							message:
								'components/ui is the presentation layer: it cannot depend on app state, data or other component layers.'
						}
					]
				}
			]
		}
	},
	{
		ignores: ['build/', '.svelte-kit/', 'static/', 'src/lib/api/schema.d.ts']
	}
);
