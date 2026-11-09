// @vitest-environment node
/// <reference types="node" />
import { existsSync, readFileSync } from 'node:fs';
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

/** Reads a PNG's width, height and whether its colour type carries alpha, from the IHDR chunk. */
function pngSize(appPath: string, file: string): [number, number, boolean] {
  const bytes = readFileSync(resolve(root, appPath, 'public', file));
  const colourType = bytes.readUInt8(25);
  return [bytes.readUInt32BE(16), bytes.readUInt32BE(20), colourType === 4 || colourType === 6];
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

  it('marks the private app noindex and links its icons from public/', () => {
    expect(html).toContain('<meta name="robots" content="noindex, nofollow" />');
    expect(html).toContain('<link rel="icon" type="image/png" href="%BASE_URL%favicon.png" />');
    expect(html).toContain(
      '<link rel="icon" type="image/png" href="%BASE_URL%favicon-dark.png" media="(prefers-color-scheme: dark)" />',
    );
    expect(html).toContain('<link rel="apple-touch-icon" href="%BASE_URL%apple-touch-icon.png" />');
    for (const icon of ['favicon.png', 'favicon-dark.png']) expect(pngSize(app.path, icon)).toEqual([64, 64, true]);
    // 180 × 180 on an opaque tile: an RGB PNG (colour type 2) carries no alpha channel.
    expect(pngSize(app.path, 'apple-touch-icon.png')).toEqual([180, 180, false]);
  });

  it('paints the splash twin inside the mount node, styled by boot-splash.css with both wordmarks', () => {
    const mount = html.slice(html.indexOf('<div id="root">'), html.indexOf('type="module"'));
    expect(mount).toContain('<div class="boot-splash" role="status"');
    expect(mount).toContain('src="%BASE_URL%ocharo-wordmark-ink.png"');
    expect(mount).toContain('src="%BASE_URL%ocharo-wordmark-reverse.png"');
    expect(mount).toContain('<div class="boot-splash-track"><div class="boot-splash-fill"></div></div>');
    for (const rule of [
      '.boot-splash {',
      '.boot-splash-wordmark-reverse {',
      'html.dark .boot-splash-wordmark-ink {',
      'html.dark .boot-splash-wordmark-reverse {',
      // The mounted splash: SDK theme `background`, `muted` and `primary`, light then dark.
      'background-color: #f5f6fa',
      'background-color: #eef0f6',
      'background-color: #8b2459',
      'background-color: #141416',
      'background-color: #262629',
      'background-color: #c24783',
    ])
      expect(stylesheet).toContain(rule);
    for (const wordmark of ['ocharo-wordmark-ink.png', 'ocharo-wordmark-reverse.png'])
      expect(existsSync(resolve(root, app.path, 'public', wordmark))).toBe(true);
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
