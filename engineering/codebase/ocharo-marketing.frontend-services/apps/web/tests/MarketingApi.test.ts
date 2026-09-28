import { describe, expect, it, vi } from 'vitest';
import { createMarketingApi, MarketingDecoders } from '../src/integration/marketing';
import { MarketingExtensions } from '../src/domain/marketing';

describe('marketing wire boundary', () => {
  it('preserves an unconfigured provider reason at the supported envelope boundary', async () => {
    const fetcher = vi.fn<typeof fetch>(async () =>
      Response.json(
        { title: 'Marketing command rejected', detail: 'This image provider is not enabled and configured.' },
        { status: 409 },
      ),
    );
    const result = await createMarketingApi(fetcher).generate({
      provider: 'openai',
      prompt: 'Test',
      referenceAssetIds: ['front'],
    });
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.failure.problem?.detail).toBe('This image provider is not enabled and configured.');
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it('rejects a successful response missing required workflow fields', async () => {
    const fetcher = vi.fn<typeof fetch>(async () =>
      Response.json({ brand: { name: 'Ocharo', timezone: 'Asia/Tashkent', handle: null }, products: [] }),
    );
    const result = await createMarketingApi(fetcher).getWorkspace();
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.failure.code).toBe('protocol');
  });

  it('preserves disconnected planning rather than converting it into delivery', async () => {
    const planned = {
      id: 'plan-1',
      draftId: 'post-1',
      draftRevision: 2,
      channel: 'instagram',
      scheduledAt: '2026-10-01T07:00:00Z',
      status: 'planned',
      message: 'Publishing is disabled.',
      externalId: null,
    };
    const fetcher = vi.fn<typeof fetch>(async () => Response.json(planned));
    const result = await createMarketingApi(fetcher).scheduleDraft('post-1', 2, 'instagram', planned.scheduledAt);
    expect(result.ok && result.value).toEqual(planned);
    expect(JSON.parse(String(fetcher.mock.calls[0]?.[1]?.body))).toEqual({
      expectedRevision: 2,
      channel: 'instagram',
      scheduledAt: planned.scheduledAt,
    });
  });

  it('does not retry a conflicted revision mutation', async () => {
    const fetcher = vi.fn<typeof fetch>(async () =>
      Response.json({ title: 'Conflict', detail: 'Stale revision.' }, { status: 409 }),
    );
    const result = await createMarketingApi(fetcher).approveDraft('post-1', 4);
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.failure.status).toBe(409);
      expect(result.failure.problem?.detail).toBe('Stale revision.');
    }
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it('keeps an unavailable metric distinct from observed zero', () => {
    const unavailable = {
      channel: 'instagram',
      name: 'views',
      value: null,
      status: 'not-connected',
      observedAt: null,
      explanation: 'No account.',
    };
    expect(MarketingDecoders.metric(unavailable)).toBe(true);
    expect(MarketingDecoders.metric({ ...unavailable, value: 0, status: 'observed' })).toBe(true);
    expect(MarketingDecoders.metric({ ...unavailable, value: '0' })).toBe(false);
  });

  it('converts Tashkent wall time and rejects calendar rollover', () => {
    expect(MarketingExtensions.toTashkentInstant('2026-10-01T12:30')).toBe('2026-10-01T07:30:00.000Z');
    expect(MarketingExtensions.toTashkentInstant('2026-02-30T12:30')).toBeNull();
    expect(MarketingExtensions.toTashkentInstant('2026-10-01T25:30')).toBeNull();
    expect(MarketingExtensions.toTashkentInstant('Tomorrow')).toBeNull();
  });
});
