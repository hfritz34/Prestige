import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from "path"
import tailwindcss from "@tailwindcss/vite"
import { copyFileSync } from 'fs'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
      react(),
      tailwindcss(),
      // Copy web.config for .NET deployment
      mode === 'landing' && {
        name: 'copy-web-config',
        writeBundle() {
          copyFileSync('web.config', 'dist/web.config')
        }
      }
  ].filter(Boolean),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    outDir: './dist',
    emptyOutDir: true, // also necessary
    rollupOptions: mode === 'landing' ? {
      input: {
        main: path.resolve(__dirname, 'landing.html')
      }
    } : undefined
  }
}))
