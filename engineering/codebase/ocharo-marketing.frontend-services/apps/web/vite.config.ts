import tailwindcss from '@tailwindcss/vite';
import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';
import mkcert from 'vite-plugin-mkcert';

export default defineConfig(({ command, mode }) => {
  const https = mode !== 'http' && process.env.VITE_HTTPS !== 'false';

  return {
    base: '/',
    plugins: [vue(), tailwindcss(), ...(command === 'serve' && https ? [mkcert()] : [])],
    build: { outDir: 'dist', emptyOutDir: true, target: 'es2022' },
    server: {
      host: 'localhost',
      port: 8242,
      strictPort: true,
      https: https ? {} : undefined,
      proxy: {
        '/api': {
          target: process.env.VITE_API_TARGET ?? 'https://localhost:8276',
          changeOrigin: false,
          secure: false,
        },
      },
    },
  };
});
