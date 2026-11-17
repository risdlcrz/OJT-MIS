import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import globals from 'globals'

/*
 * Lint rules for the Vue frontend (npm run lint).
 * Recommended rules only, so it catches real mistakes (unused variables,
 * undefined names, broken templates) without arguing about style.
 */
export default [
  {
    ignores: ['dist/**', 'node_modules/**', 'references/**', 'OJTMISApi/**', 'OJTMISApi.Tests/**', 'public/**']
  },
  js.configs.recommended,
  ...pluginVue.configs['flat/essential'],
  {
    files: ['**/*.{js,mjs,vue}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: { ...globals.browser }
    },
    rules: {
      // Page files are named after their URL (login.vue, dtr.vue), not as multi-word components.
      'vue/multi-word-component-names': 'off',
      // `catch {}` blocks that deliberately ignore an error are fine.
      'no-empty': ['error', { allowEmptyCatch: true }],
      'no-unused-vars': ['error', { argsIgnorePattern: '^_', caughtErrors: 'none' }]
    }
  },
  {
    // This core rule cannot see that <script setup> values are used in the template.
    files: ['**/*.vue'],
    rules: { 'no-useless-assignment': 'off' }
  },
  {
    files: ['vite.config.js', 'eslint.config.js'],
    languageOptions: { globals: { ...globals.node } }
  }
]
