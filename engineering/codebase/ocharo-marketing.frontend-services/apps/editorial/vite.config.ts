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
    base: '/studio/',
    plugins: [vue(), tailwindcss(), ...(command === 'serve' && https && !tls ? [mkcert()] : [])],
    build: { outDir: 'dist', emptyOutDir: true, target: 'es2022' },
    server: {
      host: 'localhost',
      port: 8284,
      strictPort: true,
      https: https ? (tls ?? {}) : undefined,
      // The API host renders the public site and previews; the Studio links to them.
      proxy: Object.fromEntries(
        [
          '^/$',
          '/api',
          '/preview',
          '/media',
          '/journal',
          '/topics',
          '/site.css',
          '/feed.xml',
          '/atlas',
          '/wear',
          '/catalogue',
          '/uz',
          '/ru',
          '/sitemap.xml',
          '/robots.txt',
        ].map((path) => [
          path,
          { target: process.env.VITE_API_TARGET ?? 'https://localhost:8280', changeOrigin: false, secure: true },
        ]),
      ),
    },
  };
});
