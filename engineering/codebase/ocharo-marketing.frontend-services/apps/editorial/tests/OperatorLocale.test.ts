import { mount } from '@vue/test-utils';
import { defineComponent, h, nextTick } from 'vue';
import { describe, expect, it } from 'vitest';
import { provideLocale } from '@wow-two-beta/ui-vue/foundation/i18n';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import SectionTextEditor from '../src/presentation/blog/components/SectionTextEditor.vue';
import type { ArticleSection } from '../src/domain/blog/BlogModels';
describe('editorial operator language and authored text', () => {
  it('changes the real SDK controls without rewriting content or emitting an edit', async () => {
    const operator = useUiLocale();
    operator.setUiLocale('en');
    const section: ArticleSection = {
      heading: 'Title',
      paragraphs: ['Title **Save**'],
      list: null,
      sourceIndexes: [],
      format: 'markdown',
    };
    const root = defineComponent({
      setup() {
        provideLocale(operator.uiLocale, operator.sdkMessages);
        return () => h(SectionTextEditor, { section, id: 'story-text' });
      },
    });
    const wrapper = mount(root),
      editor = wrapper.getComponent(SectionTextEditor);
    expect(wrapper.find('button[aria-label="Bold"]').exists()).toBe(true);
    operator.setUiLocale('ru');
    await nextTick();
    expect(wrapper.find('button[aria-label="Жирный"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Форматированный текст');
    expect((wrapper.get('textarea').element as HTMLTextAreaElement).value).toBe('Title **Save**');
    expect(editor.emitted('change')).toBeUndefined();
    expect(section.heading).toBe('Title');
    operator.setUiLocale('uz');
    await nextTick();
    expect(wrapper.find('button[aria-label="Qalin"]').exists()).toBe(true);
    expect((wrapper.get('textarea').element as HTMLTextAreaElement).value).toBe('Title **Save**');
    expect(editor.emitted('change')).toBeUndefined();
    wrapper.unmount();
  });
});
