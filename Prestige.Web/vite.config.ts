import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from "path"
import tailwindcss from "@tailwindcss/vite"
import { copyFileSync, renameSync, existsSync } from 'fs'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
      react(),
      tailwindcss(),
      // Copy web.config for .NET deployment and rename landing.html to index.html
      mode === 'landing' && {
        name: 'copy-configs',
        writeBundle() {
          copyFileSync('web.config', 'dist/web.config')
          copyFileSync('staticwebapp.config.json', 'dist/staticwebapp.config.json')
          // Rename landing.html to index.html for Azure Static Web Apps
          if (existsSync('dist/landing.html')) {
            renameSync('dist/landing.html', 'dist/index.html')
          }
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
