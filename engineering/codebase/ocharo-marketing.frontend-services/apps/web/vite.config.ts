import { readFileSync } from 'node:fs';
import tailwindcss from '@tailwindcss/vite';
import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';
import mkcert from 'vite-plugin-mkcert';

export default defineConfig(({ command, mode }) => {
  const https = mode !== 'http' && process.env.VITE_HTTPS !== 'false';
  const tls =
    process.env.OCHARO_TLS_CERT && process.env.OCHARO_TLS_KEY
      ? { cert: readFileSync(process.env.OCHARO_TLS_CERT), key: readFileSync(process.env.OCHARO_TLS_KEY) }
      : undefined;

  return {
    define: {
      __APP_VERSION__: JSON.stringify(
        process.env.APP_VERSION && process.env.APP_VERSION !== '0.0.0' ? process.env.APP_VERSION : 'dev',
      ),
    },
    base: '/',
    plugins: [vue(), tailwindcss(), ...(command === 'serve' && https && !tls ? [mkcert()] : [])],
    build: { outDir: 'dist', emptyOutDir: true, target: 'es2022' },
    server: {
      host: 'localhost',
      port: 8358,
      strictPort: true,
      https: https ? (tls ?? {}) : undefined,
      proxy: {
        '/api': {
          target: process.env.VITE_API_TARGET ?? 'https://localhost:8276',
          changeOrigin: false,
          secure: true,
        },
      },
    },
  };
});
