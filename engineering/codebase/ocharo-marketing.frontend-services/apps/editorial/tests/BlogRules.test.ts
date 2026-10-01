import { describe, expect, it } from 'vitest';
import {
  emptyArticle,
  publishBlockers,
  readingStats,
  slugify,
  splitLines,
  splitParagraphs,
} from '../src/domain/blog/BlogRules';

describe('blog rules', () => {
  it('turns titles into public addresses', () => {
    expect(slugify('Why do we love clothes we never wear?')).toBe('why-do-we-love-clothes-we-never-wear');
    expect(slugify('  Ko‘ylak — yangi to‘plam! ')).toBe('koylak-yangi-toplam');
  });

  it('splits paragraphs on blank lines and joins soft line breaks', () => {
    expect(splitParagraphs('One line\ncontinues.\n\n\nSecond.')).toEqual(['One line continues.', 'Second.']);
    expect(splitLines('a\n\n b \n')).toEqual(['a', 'b']);
    expect(splitLines('  ')).toBeNull();
  });

  it('lists what blocks publication', () => {
    const draft = emptyArticle('style');
    expect(publishBlockers(draft)).toEqual(['qisqa tavsif', 'kirish', 'kamida bitta bo‘lim va paragraf']);
    draft.excerpt = 'x';
    draft.intro = 'y';
    draft.sections = [{ heading: 'h', paragraphs: ['p'], list: null, sourceIndexes: null }];
    draft.cover = { mediaSha256: 'a', alt: '', caption: '', cardClass: null };
    expect(publishBlockers(draft)).toEqual(['muqova tavsifi va izohi']);
  });

  it('estimates reading time at 200 words a minute', () => {
    const draft = emptyArticle('style');
    draft.intro = Array.from({ length: 401 }, () => 'word').join(' ');
    expect(readingStats(draft)).toEqual({ words: 401, minutes: 3 });
  });
});
