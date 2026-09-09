import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    ],

  // Prevent Vite from watching Visual Studio's .vs folder which can contain
  // locked/index files (e.g. .vsidx) and cause EBUSY errors on Windows.
  server: {
    watch: {
      // ignore the .vs folder and any .vsidx files
      // include both glob patterns and regex to match Windows backslashes
      ignored: [
        '**/.vs/**',
        '**/*.vsidx',
        /(^|[\\/])\.vs([\\/]|$)/,
        /\.vsidx$/i
      ]
    }
  },

  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
