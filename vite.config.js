import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath } from 'url'
import { dirname, resolve } from 'path'

const __dirname = dirname(fileURLToPath(import.meta.url))

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            '@': resolve(__dirname, './src')
        }
    },
    server: {
        // Forwards /api to the .NET backend. So src/services/api.js needs no
        // hard-coded port, and there are no CORS problems.
        // The port must match OJTMISApi (Properties/launchSettings.json).
        proxy: {
            '/api': {
                target: process.env.OJT_API_URL || 'http://localhost:5198',
                changeOrigin: true
            }
        },
        watch: {
            ignored: [
                '**/.vs/**',
                '**/node_modules/**',
                '**/dist/**'
            ]
        }
    }
})