import { proxyUnavailable } from './dev/ProxyProblem';
import { Agent } from 'node:https';
import { readFileSync } from 'node:fs';
import tailwindcss from '@tailwindcss/vite';
import vue from '@vitejs/plugin-vue';
import { defineConfig, type Plugin } from 'vite';
import mkcert from 'vite-plugin-mkcert';
import { colorModeScript } from '@wow-two-beta/fe/foundation/primitives';
import { ThemeStorageKey } from './src/bootstrap/theme';

/** Serves and emits `/theme.js`: the SDK's pre-paint colour script, so a dark Marketing never flashes light first. */
function themeScript(): Plugin {
  const source = colorModeScript({ storageKey: ThemeStorageKey });
  return {
    name: 'ocharo-marketing-theme-script',
    configureServer(server) {
      server.middlewares.use('/theme.js', (_request, response) => {
        response.setHeader('Content-Type', 'text/javascript');
        response.end(source);
      });
    },
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'theme.js', source });
    },
  };
}

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
    plugins: [vue(), tailwindcss(), themeScript(), ...(command === 'serve' && !tls ? [mkcert()] : [])],
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
          configure: (proxy) => {
            proxy.on('error', proxyUnavailable);
          },
          ...(command === 'serve' && process.env.VITE_API_CA_PATH
            ? { agent: new Agent({ ca: readFileSync(process.env.VITE_API_CA_PATH) }) }
            : {}),
        },
      },
    },
  };
});
