import { ESLint } from 'eslint';
import { describe, expect, it } from 'vitest';

const eslint = new ESLint();

describe('application boundaries', () => {
  it('rejects a presentation import from application orchestration', async () => {
    const [result] = await eslint.lintText("import Page from '../../presentation/blog/StoriesPage.vue'; void Page;", {
      filePath: 'src/application/blog/Invalid.ts',
    });
    expect(result?.messages.some((message) => message.ruleId === 'no-restricted-imports')).toBe(true);
  });

  it('rejects an integration import from presentation', async () => {
    const [result] = await eslint.lintText("import { blogApi } from '../../integration/blog'; void blogApi;", {
      filePath: 'src/presentation/blog/Invalid.ts',
    });
    expect(result?.messages.some((message) => message.ruleId === 'no-restricted-imports')).toBe(true);
  });
});
