import { describe, expect, it } from 'vitest';
import { MarketingDecoders } from '../src/integration/marketing';

const settings = {
  slots: [{ id: 'tg', channel: 'telegram', days: [1, 3, 5], time: '19:00', enabled: true }],
  draftNewCatalogueProducts: false,
  defaultPresetId: 't02-bone',
  defaultChannels: ['instagram', 'telegram'],
  defaultLocale: 'uz',
  captionTemplates: { uz: '{name}' },
  autoSyncHours: 0,
  autoFillDays: 14,
};

describe('automation wire contracts', () => {
  it('accepts the automation settings shape and rejects a missing slot field', () => {
    expect(MarketingDecoders.automation(settings)).toBe(true);
    expect(
      MarketingDecoders.automation({
        ...settings,
        slots: [{ id: 'tg', channel: 'telegram', days: [1], enabled: true }],
      }),
    ).toBe(false);
  });

  it('accepts sync, fill and story payloads', () => {
    expect(
      MarketingDecoders.syncReport({
        received: 29,
        created: 29,
        updated: 0,
        imagesStored: 58,
        newProductIds: [],
        draftIds: [],
        warnings: [],
      }),
    ).toBe(true);
    expect(MarketingDecoders.fillReport({ planned: [], skipped: ['x'] })).toBe(true);
    expect(
      MarketingDecoders.stories([
        { title: 'T', link: 'http://b/journal/t/', excerpt: 'E', topic: null, publishedAt: null, draftId: null },
      ]),
    ).toBe(true);
    expect(
      MarketingDecoders.automationRun({
        lastSyncAt: null,
        lastFillAt: null,
        lastFillPlanned: 0,
        lastError: null,
        lastErrorAt: null,
      }),
    ).toBe(true);
  });
});
