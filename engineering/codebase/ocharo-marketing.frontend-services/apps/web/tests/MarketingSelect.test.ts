import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import LanguagePicker from '@ocharomarketing/connect/LanguagePicker.vue';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

const mounted: VueWrapper[] = [];
beforeEach(() => {
  vi.stubGlobal(
    'ResizeObserver',
    class {
      observe() {}
      unobserve() {}
      disconnect() {}
    },
  );
  Element.prototype.scrollIntoView = vi.fn();
  useUiLocale().setUiLocale('en');
});
afterEach(() => {
  mounted.splice(0).forEach((wrapper) => wrapper.unmount());
  document.body.innerHTML = '';
  vi.unstubAllGlobals();
});
const options = [
  { value: 'a', label: 'Alpha' },
  { value: 'b', label: 'Beta' },
  { value: 'c', label: 'Gamma', disabled: true },
];
function picker(props = {}) {
  const wrapper = mount(MarketingSelect, {
    attachTo: document.body,
    props: { modelValue: 'a', options, label: 'Example', ...props },
  });
  mounted.push(wrapper);
  return wrapper;
}
async function settle() {
  await nextTick();
  await flushPromises();
}

describe('Marketing SDK dropdown', () => {
  it('renders an SDK listbox, emits a value, and waits for confirmed model updates', async () => {
    const wrapper = picker();
    expect(wrapper.find('select').exists()).toBe(false);
    expect(wrapper.get('button[aria-label="Example"]').text()).toContain('Alpha');
    await wrapper.get('button[aria-label="Example"]').trigger('click');
    await settle();
    const choices = [...document.querySelectorAll<HTMLElement>('[role="option"]')];
    expect(choices.map((item) => item.textContent?.trim())).toEqual(['Alpha', 'Beta', 'Gamma']);
    choices[1]!.click();
    await settle();
    expect(wrapper.emitted('update:modelValue')).toEqual([['b']]);
    expect(wrapper.get('button[aria-label="Example"]').text()).toContain('Alpha');
    await wrapper.setProps({ modelValue: 'b' });
    expect(wrapper.get('button[aria-label="Example"]').text()).toContain('Beta');
  });
  it('keeps disabled and loading controls closed', async () => {
    const wrapper = picker({ disabled: true });
    expect(wrapper.get('button').attributes('disabled')).toBeDefined();
    await wrapper.get('button').trigger('click');
    await settle();
    expect(document.querySelector('[role="listbox"]')).toBeNull();
    await wrapper.setProps({ disabled: false, loading: true });
    expect(wrapper.get('button').attributes('disabled')).toBeDefined();
  });
  it('preserves required form validity and focuses its labelled trigger', async () => {
    const wrapper = picker({ modelValue: '', required: true });
    const input = wrapper.get('input[required]').element as HTMLInputElement;
    expect(input.checkValidity()).toBe(false);
    await settle();
    expect(document.activeElement).toBe(wrapper.get('button[aria-label="Example"]').element);
    await wrapper.setProps({ modelValue: 'a' });
    expect(input.checkValidity()).toBe(true);
    await wrapper.setProps({ options: [{ value: 'b', label: 'Beta' }] });
    expect(input.checkValidity()).toBe(false);
  });
  it('opens the language menu and dismisses without changing locale', async () => {
    const wrapper = mount(LanguagePicker, { attachTo: document.body });
    mounted.push(wrapper);
    const trigger = wrapper.get('button[aria-label="Interface language"]');
    (trigger.element as HTMLButtonElement).focus();
    await trigger.trigger('click');
    await settle();
    expect(document.querySelector('[role="listbox"]')).not.toBeNull();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await settle();
    expect(useUiLocale().uiLocale.value).toBe('en');
  });
});

it('preserves a numeric zero selection as a valid required value', async () => {
  const wrapper = mount(MarketingSelect, {
    attachTo: document.body,
    props: {
      modelValue: 0,
      options: [
        { value: 0, label: 'None' },
        { value: 1, label: 'One' },
      ],
      label: 'Amount',
      required: true,
    },
  });
  mounted.push(wrapper);
  expect((wrapper.get('input[required]').element as HTMLInputElement).checkValidity()).toBe(true);
  await wrapper.get('button').trigger('click');
  await settle();
  document.querySelectorAll<HTMLElement>('[role="option"]')[1]!.click();
  await settle();
  expect(wrapper.emitted('update:modelValue')).toEqual([[1]]);
});
it('rejects an open-menu selection when its parent fieldset becomes disabled', async () => {
  const wrapper = mount(
    {
      components: { MarketingSelect },
      props: { locked: Boolean },
      data: () => ({ chosen: 'a', options }),
      template:
        '<fieldset :disabled="locked"><MarketingSelect v-model="chosen" :options="options" label="Guarded" /></fieldset>',
    },
    { attachTo: document.body, props: { locked: false } },
  );
  mounted.push(wrapper);
  await wrapper.get('button').trigger('click');
  await settle();
  await wrapper.setProps({ locked: true });
  document.querySelectorAll<HTMLElement>('[role="option"]')[1]!.click();
  await settle();
  expect(wrapper.getComponent(MarketingSelect).emitted('update:modelValue')).toBeUndefined();
  expect(wrapper.get('button').text()).toContain('Alpha');
});
