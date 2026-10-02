import { proxyUnavailable } from './dev/ProxyProblem';
import { Agent } from 'node:https';
import { readFileSync } from 'node:fs';
import tailwindcss from '@tailwindcss/vite';
import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';
import mkcert from 'vite-plugin-mkcert';

/** Keeps development proxy overrides on verified HTTPS endpoints. */
function httpsTarget(value: string): string {
  if (new URL(value).protocol !== 'https:') throw new Error('The development API target must use HTTPS.');
  return value;
}

export default defineConfig(({ command }) => {
  if (command === 'serve' && Boolean(process.env.OCHARO_TLS_CERT) !== Boolean(process.env.OCHARO_TLS_KEY))
    throw new Error('Provide both OCHARO_TLS_CERT and OCHARO_TLS_KEY.');
  const tls =
    command === 'serve' && process.env.OCHARO_TLS_CERT && process.env.OCHARO_TLS_KEY
      ? { cert: readFileSync(process.env.OCHARO_TLS_CERT), key: readFileSync(process.env.OCHARO_TLS_KEY) }
      : undefined;

  return {
    define: {
      __APP_VERSION__: JSON.stringify(
        process.env.APP_VERSION && process.env.APP_VERSION !== '0.0.0' ? process.env.APP_VERSION : 'dev',
      ),
    },
    base: '/',
    plugins: [vue(), tailwindcss(), ...(command === 'serve' && !tls ? [mkcert()] : [])],
    build: { outDir: 'dist', emptyOutDir: true, target: 'es2022' },
    server: {
      host: 'localhost',
      port: 8358,
      strictPort: true,
      https: tls ?? {},
      proxy: {
        '/api': {
          target: httpsTarget(process.env.VITE_API_TARGET ?? 'https://localhost:8276'),
          changeOrigin: false,
          secure: true,
          configure: (proxy) => { proxy.on('error', proxyUnavailable); },
          ...(command === 'serve' && process.env.VITE_API_CA_PATH
            ? { agent: new Agent({ ca: readFileSync(process.env.VITE_API_CA_PATH) }) }
            : {}),
        },
      },
    },
  };
});
