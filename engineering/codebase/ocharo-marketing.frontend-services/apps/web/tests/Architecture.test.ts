import { ESLint } from 'eslint';
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
