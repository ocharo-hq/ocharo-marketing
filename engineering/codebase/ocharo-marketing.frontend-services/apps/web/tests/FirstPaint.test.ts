/// <reference types="node" />
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { runInNewContext } from 'node:vm';
import { expect, it, vi } from 'vitest';
const root = resolve(process.cwd(), '../..');
const paths = ['apps/web', 'apps/editorial'];
it.each(paths)('boots external first-paint assets before the app for %s', (path) => {
  const html = readFileSync(resolve(root, path, 'index.html'), 'utf8');
  expect(html.indexOf('theme.js')).toBeLessThan(html.indexOf('src="/src/'));
  expect(html).toContain('%BASE_URL%boot-splash.css');
  const css = readFileSync(resolve(root, path, 'public/boot-splash.css'), 'utf8');
  expect(css).toMatch(/html/);
  expect(css).toMatch(/body/);
  expect(css).toContain('background');
  const style: { colorScheme?: string } = {};
  runInNewContext(readFileSync(resolve(root, path, 'public/theme.js'), 'utf8'), {
    document: { documentElement: { classList: { toggle: vi.fn() }, style } },
    localStorage: { getItem: () => null },
    sessionStorage: { getItem: () => null },
    location: { pathname: '/' },
  });
  expect(style.colorScheme).toBe('light');
});
