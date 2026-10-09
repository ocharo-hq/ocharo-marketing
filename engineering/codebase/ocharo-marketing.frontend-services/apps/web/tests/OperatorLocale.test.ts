import { mount } from '@vue/test-utils';
import { nextTick } from 'vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import LanguagePicker from '@ocharomarketing/connect/LanguagePicker.vue';
import { initializeUiLocale, operatorLocaleKey, useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { translateUi, uiMessages } from '@ocharomarketing/connect/UiMessages';
import StatusIndicator from '../src/presentation/marketing/StatusIndicator.vue';
const locale = useUiLocale();
beforeEach(() => {
  localStorage.clear();
  locale.setUiLocale('en');
});
describe('operator locale', () => {
  it('persists a visible language choice and updates existing workflow labels', async () => {
    const picker = mount(LanguagePicker),
      status = mount(StatusIndicator, { props: { status: 'draft' } });
    expect(status.text()).toBe('Draft');
    picker.getComponent({ name: 'MarketingSelect' }).vm.$emit('update:modelValue', 'ru');
    await nextTick();
    expect(localStorage.getItem(operatorLocaleKey)).toBe('ru');
    expect(document.documentElement.lang).toBe('ru');
    expect(status.text()).toBe('Черновик');
    expect(picker.get('button[aria-haspopup="listbox"]').attributes('aria-label')).toBe('Язык интерфейса');
    picker.getComponent({ name: 'MarketingSelect' }).vm.$emit('update:modelValue', 'uz');
    await nextTick();
    expect(status.text()).toBe('Qoralama');
    picker.unmount();
    status.unmount();
  });
  it('restores a supported saved choice and rejects unsupported values', () => {
    localStorage.setItem(operatorLocaleKey, 'ru');
    initializeUiLocale();
    expect(locale.uiLocale.value).toBe('ru');
    locale.setUiLocale('fr');
    expect(locale.uiLocale.value).toBe('ru');
    localStorage.setItem(operatorLocaleKey, 'broken');
    initializeUiLocale();
    expect(locale.uiLocale.value).toBe('en');
  });
  it('switches without storage access and preserves message variables', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('denied');
    });
    locale.setUiLocale('ru');
    await nextTick();
    expect(locale.t('View {title}', { title: 'Title' })).toBe('Открыть Title');
    expect(translateUi('“Title” bo‘limi saqlandi.', 'ru')).toBe('Раздел «Title» сохранён.');
    expect(translateUi('Xizmat 409 xato qaytardi.', 'en')).toBe('The service returned error 409.');
  });
  it('supplies translated SDK messages and usable Uzbek calendar labels', () => {
    locale.setUiLocale('uz');
    expect(locale.sdkMessages.value['ImageEditor.removeBackground']).toBe('Fonni olib tashlash');
    expect(locale.sdkMessages.value['MarkdownEditor.bold']).toBe('Qalin');
    expect(locale.formatWhen('2026-10-01T10:00:00Z')).toContain('okt');
    expect(locale.formatWhen('2026-10-01T10:00:00Z')).toContain('15:00');
    expect(locale.sdkMessages.value['Unowned.futureKey']).toBeUndefined();
  });
  it('has nonempty Uzbek and Russian messages for every operator message', () => {
    for (const [source, translations] of Object.entries(uiMessages)) {
      expect(translations.uz, source).toBeTruthy();
      expect(translations.ru, source).toBeTruthy();
    }
  });
});
