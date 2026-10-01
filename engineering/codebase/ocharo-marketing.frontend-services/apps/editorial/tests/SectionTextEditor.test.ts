import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import SectionTextEditor from '../src/presentation/blog/components/SectionTextEditor.vue';
import type { ArticleSection } from '../src/domain/blog/BlogModels';
const section: ArticleSection = {
  heading: 'Story',
  paragraphs: ['Plain **stars**'],
  list: ['Item'],
  sourceIndexes: [0],
};
const stub = {
  name: 'MarkdownEditor',
  props: ['modelValue', 'showImages', 'isDisabled'],
  emits: ['update:modelValue'],
  template:
    '<textarea data-markdown :value="modelValue" :disabled="isDisabled" @input="$emit(\'update:modelValue\', $event.target.value)" />',
};
const mountEditor = (source: ArticleSection, disabled = false) =>
  mount(SectionTextEditor, {
    props: { section: source, id: 'section-one', disabled },
    global: { stubs: { MarkdownEditor: stub } },
  });
describe('section text editing', () => {
  it('does not convert saved plain prose until the explicit formatting action', async () => {
    const wrapper = mountEditor(section);
    expect(wrapper.find('[data-markdown]').exists()).toBe(false);
    expect(wrapper.emitted('change')).toBeUndefined();
    await wrapper.get('button').trigger('click');
    const formatted = wrapper.emitted('change')![0]![0] as ArticleSection;
    expect(formatted.format).toBe('markdown');
    expect(formatted.paragraphs[0]).toContain('\\*\\*stars\\*\\*');
    await wrapper.setProps({ section: formatted });
    expect(wrapper.find('[data-markdown]').exists()).toBe(true);
    expect(wrapper.getComponent({ name: 'MarkdownEditor' }).props('showImages')).toBe(false);
    wrapper.unmount();
  });
  it('tracks plain prose immediately without collapsing the typed paragraph break', async () => {
    const wrapper = mountEditor(section);
    await wrapper.get('#section-one').setValue('First\n\n');
    const next = wrapper.emitted('change')![0]![0] as ArticleSection;
    expect(next.paragraphs).toEqual(['First']);
    expect(next.format).toBeUndefined();
    await wrapper.setProps({ section: next });
    expect((wrapper.get('#section-one').element as HTMLTextAreaElement).value).toBe('First\n\n');
    await wrapper.get('#section-one').setValue('First\n\nSecond');
    expect((wrapper.emitted('change')!.at(-1)![0] as ArticleSection).paragraphs).toEqual(['First', 'Second']);
    wrapper.unmount();
  });
  it('updates the draft immediately without flattening fenced code, quotes, or hard breaks', async () => {
    const wrapper = mountEditor({ ...section, format: 'markdown', list: null });
    const value = '> quote\n\n```text\nfirst\nsecond\n```\n\nline  \nbreak';
    await wrapper.get('[data-markdown]').setValue(value);
    expect((wrapper.emitted('change')![0]![0] as ArticleSection).paragraphs).toEqual([value]);
    wrapper.unmount();
  });
  it('blocks changes while locked and shows over-limit feedback without discarding entered text', async () => {
    const wrapper = mountEditor(section, true);
    await wrapper.get('button').trigger('click');
    expect(wrapper.emitted('change')).toBeUndefined();
    await wrapper.setProps({ section: { ...section, format: 'markdown', paragraphs: ['a'.repeat(32001)] } });
    expect(wrapper.get('[role=alert]').text()).toContain('32,001');
    expect(wrapper.get('[data-markdown]').attributes('disabled')).toBeDefined();
    wrapper.unmount();
  });
});
