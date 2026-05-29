import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5026',
        changeOrigin: true,
      },
    },
  },
})
// frontend: http://localhost:5173
// backend: http://localhost:5026
// postgres: localhost:5432
// mongo: localhost:27017
