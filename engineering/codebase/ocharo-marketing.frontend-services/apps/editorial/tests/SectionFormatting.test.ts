import { describe, expect, it } from 'vitest';
import { formatSection, markdownParagraphs } from '../src/domain/blog/SectionFormatting';
import { emptyArticle, toInput } from '../src/domain/blog/BlogRules';
import type { ArticleDocument, ArticleSection } from '../src/domain/blog/BlogModels';

const plain: ArticleSection = {
  heading: 'A detail',
  paragraphs: ['A **literal** note.', '<b>still text</b>'],
  list: ['One', 'Two [literal]'],
  sourceIndexes: [0],
};
describe('explicit section formatting', () => {
  it('preserves literal content and citations when the author opts in', () => {
    const formatted = formatSection(plain);
    expect(formatted.format).toBe('markdown');
    expect(formatted.list).toBeNull();
    expect(formatted.paragraphs[0]).toContain('\\*\\*literal\\*\\*');
    expect(formatted.paragraphs[0]).toContain('\\<b\\>still text\\</b\\>');
    expect(formatted.paragraphs[0]).toContain('- One\n- Two \\[literal\\]');
    formatted.sourceIndexes!.push(1);
    expect(plain.sourceIndexes).toEqual([0]);
    expect(plain.format).toBeUndefined();
  });
  it('preserves meaningful Markdown whitespace while treating an empty draft as empty', () => {
    const text = '    code\n\nText  \nline\n';
    expect(markdownParagraphs(text)).toEqual([text]);
    expect(markdownParagraphs('   \n')).toEqual([]);
  });
  it('does not reinterpret old documents while round-tripping new formatted revisions', () => {
    const original = {
      ...emptyArticle('style'),
      sections: [plain],
      status: 'draft',
      publishAt: null,
      publishedAt: null,
      translationKey: null,
    } as ArticleDocument;
    expect(toInput(original).sections[0]!.format).toBeUndefined();
    original.sections = [formatSection(plain)];
    const draft = toInput(original, 5);
    expect(draft.sections).toEqual(original.sections);
    draft.sections[0]!.paragraphs.push('More');
    expect(original.sections[0]!.paragraphs).toHaveLength(1);
  });
});
