import { ESLint } from 'eslint';
import { parse } from '@vue/compiler-sfc';
import { describe, expect, it } from 'vitest';

const eslint = new ESLint();

describe('application boundaries', () => {
  it('rejects a presentation import from application orchestration', async () => {
    const [result] = await eslint.lintText(
      "import Page from '../../presentation/marketing/pages/TodayPage.vue'; void Page;",
      { filePath: 'src/application/marketing/Invalid.ts' },
    );
    expect(result?.messages.some((message) => message.ruleId === 'no-restricted-imports')).toBe(true);
  });

  it('allows the marketing integration seam', async () => {
    const [result] = await eslint.lintText(
      "import { marketingApi } from '../../integration/marketing'; void marketingApi;",
      { filePath: 'src/application/marketing/Valid.ts' },
    );
    expect(result?.messages).toEqual([]);
  });
});

it('uses styled SDK dropdowns throughout Marketing, Editorial and shared controls', () => {
  const sources = import.meta.glob<string>(
    [
      '../src/presentation/**/*.vue',
      '../../editorial/src/presentation/**/*.vue',
      '../../../packages/connect/src/**/*.vue',
    ],
    {
      eager: true,
      query: '?raw',
      import: 'default',
    },
  );
  const violations = Object.entries(sources).flatMap(([filename, source]) => {
    const template = parse(source, { filename }).descriptor.template?.content ?? '';
    return /<select(?=[\s/>])/i.test(template.replace(/<!--[\s\S]*?-->/g, '')) ? [filename] : [];
  });
  expect(Object.keys(sources).length).toBeGreaterThan(20);
  expect(violations).toEqual([]);
});
