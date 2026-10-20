// @vitest-environment node
/// <reference types="node" />
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { runInNewContext } from 'node:vm';
import { describe, expect, it } from 'vitest';
import { colorModeScript } from '@wow-two-beta/fe/foundation/primitives';
import { ThemeStorageKey as MarketingThemeKey } from '../src/bootstrap/theme';
import { ThemeStorageKey as EditorialThemeKey } from '../../editorial/src/bootstrap/theme';

const root = resolve(process.cwd(), '../..');
const apps = [
  { name: 'Marketing', path: 'apps/web', storageKey: MarketingThemeKey, expectedKey: 'ocharo.marketing.theme' },
  { name: 'Editorial', path: 'apps/editorial', storageKey: EditorialThemeKey, expectedKey: 'ocharo.editorial.theme' },
] as const;

/** Runs a generated pre-paint script against a stored value and the device's colour preference. */
function paint(script: string, getItem: () => string | null, systemDark: boolean) {
  const classes = new Set<string>();
  const style: { colorScheme?: string } = {};
  runInNewContext(script, {
    localStorage: { getItem },
    matchMedia: () => ({ matches: systemDark }),
    document: {
      documentElement: {
        style,
        classList: {
          toggle: (name: string, enabled: boolean) => (enabled ? classes.add(name) : classes.delete(name)),
        },
      },
    },
  });
  return { classes: [...classes], colorScheme: style.colorScheme };
}

describe.each(apps)('$name first paint', (app) => {
  const script = colorModeScript({ storageKey: app.storageKey });
  const html = readFileSync(resolve(root, app.path, 'index.html'), 'utf8');
  const stylesheet = readFileSync(resolve(root, app.path, 'public/boot-splash.css'), 'utf8');

  it.each([
    ['dark', false, 'dark'],
    ['dark', true, 'dark'],
    ['light', true, 'light'],
    ['system', true, 'dark'],
    ['system', false, 'light'],
    [null, true, 'dark'],
    [null, false, 'light'],
    ['invalid', false, 'light'],
  ])('honors the stored %s with system dark %s before the app module', (saved, systemDark, expected) => {
    const result = paint(script, () => saved, systemDark);
    expect(result.classes).toEqual(expected === 'dark' ? ['dark'] : []);
    expect(result.colorScheme).toBe(expected);
  });

  it('reads its own storage key, the one the colour-mode provider writes', () => {
    expect(app.storageKey).toBe(app.expectedKey);
    expect(script).toContain(JSON.stringify(app.expectedKey));
  });

  it('loads the SDK theme, the pre-paint script and the splash colours before the application, with no inline code', () => {
    const application = html.indexOf('type="module"');
    expect(html).toMatch(/<html[^>]*class="theme-ocharo"/);
    expect(html.indexOf('src="%BASE_URL%theme.js"')).toBeGreaterThan(-1);
    expect(html.indexOf('src="%BASE_URL%theme.js"')).toBeLessThan(application);
    expect(html.indexOf('href="%BASE_URL%boot-splash.css"')).toBeLessThan(application);
    expect(html).not.toMatch(/<style|<script(?![^>]*src=)/);
    expect(html).toContain('<meta name="theme-color" content="#f5f6fa" media="(prefers-color-scheme: light)" />');
    expect(html).toContain('<meta name="theme-color" content="#141416" media="(prefers-color-scheme: dark)" />');
    expect(stylesheet).toContain('background: #f5f6fa');
    expect(stylesheet).toContain('html.dark');
    expect(stylesheet).toContain('background: #141416');
  });

  it('leaves the page to the provider when browser storage is unavailable', () => {
    const result = paint(
      script,
      () => {
        throw new Error('Storage denied');
      },
      true,
    );
    expect(result.classes).toEqual([]);
  });
});
