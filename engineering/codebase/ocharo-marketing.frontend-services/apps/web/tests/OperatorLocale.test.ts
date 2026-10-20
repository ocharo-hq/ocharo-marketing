import { flushPromises, mount } from '@vue/test-utils';
import { defineComponent, h, nextTick } from 'vue';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { provideLocale } from '@wow-two-beta/fe/foundation/i18n';
import { AccountMenu } from '@wow-two-beta/fe/presentation/nav';
import {
  initializeUiLocale,
  interfaceLanguages,
  operatorLocaleKey,
  useUiLocale,
} from '@ocharomarketing/connect/UseUiLocale';
import { translateUi, uiMessages } from '@ocharomarketing/connect/UiMessages';
import StatusIndicator from '../src/presentation/marketing/StatusIndicator.vue';
const locale = useUiLocale();
beforeEach(() => {
  localStorage.clear();
  locale.setUiLocale('en');
});
afterEach(() => {
  document.body.innerHTML = '';
});

/** The frame's language rows: the SDK account menu over the operator locale, as both AppLayouts bind it. */
const LanguageRows = defineComponent({
  setup() {
    const { uiLocale, setUiLocale, sdkMessages } = useUiLocale();
    provideLocale(uiLocale, sdkMessages);
    return () =>
      h(AccountMenu, {
        locales: interfaceLanguages,
        locale: uiLocale.value,
        showAppearance: false,
        'onUpdate:locale': setUiLocale,
      });
  },
});

/** Opens the account menu and picks the language row named by its `lang`. */
async function pickLanguage(wrapper: ReturnType<typeof mount>, value: string) {
  await wrapper.get('button[aria-haspopup="menu"]').trigger('click');
  await flushPromises();
  document.querySelector<HTMLElement>(`[role="menuitemradio"][lang="${value}"]`)!.click();
  await flushPromises();
}

describe('operator locale', () => {
  it('persists a visible language choice and updates existing workflow labels', async () => {
    const menu = mount(LanguageRows, { attachTo: document.body }),
      status = mount(StatusIndicator, { props: { status: 'draft' } });
    expect(status.text()).toBe('Draft');
    await pickLanguage(menu, 'ru');
    expect(localStorage.getItem(operatorLocaleKey)).toBe('ru');
    expect(document.documentElement.lang).toBe('ru');
    expect(status.text()).toBe('Черновик');
    await menu.get('button[aria-haspopup="menu"]').trigger('click');
    await flushPromises();
    expect(document.querySelector('[data-account-menu-languages]')?.textContent).toContain('Язык');
    expect(document.querySelector('[role="menuitemradio"][lang="ru"]')?.getAttribute('aria-checked')).toBe('true');
    document.querySelector<HTMLElement>('[role="menuitemradio"][lang="uz"]')!.click();
    await nextTick();
    expect(status.text()).toBe('Qoralama');
    menu.unmount();
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
