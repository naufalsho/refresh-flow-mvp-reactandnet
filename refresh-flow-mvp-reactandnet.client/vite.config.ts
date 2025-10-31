import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        port: 5173,
        proxy: {
            '/auth': {
                target: process.env.VITE_API_BASE || 'https://localhost:5001',
                changeOrigin: true,
                secure: false,
            },
            '/me': {
                target: process.env.VITE_API_BASE || 'https://localhost:5001',
                changeOrigin: true,
                secure: false,
            }
        }
    }
})
