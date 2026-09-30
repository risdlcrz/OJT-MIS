import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
    plugins: [vue()],
    server: {
        // Ipinapasa ang /api papunta sa .NET backend. Kaya hindi na kailangan
        // ng hard-coded na port sa src/services/api.js at walang CORS problem.
        // Dapat match ang port ng OJTMISApi (Properties/launchSettings.json).
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