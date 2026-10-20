import { afterEach, describe, expect, it, vi } from 'vitest';
import { NoticeTone } from '@wow-two-beta/fe/feedback';
import { requestFailureMessage } from '../../../packages/connect/src/RequestFeedback';
import { useUiLocale } from '../../../packages/connect/src/UseUiLocale';
import { feedbackBus, sessionExpiryNotice } from '@ocharomarketing/connect/Feedback';

afterEach(() => {
  useUiLocale().setUiLocale('en');
});

describe('session expiry feedback', () => {
  it('keeps a 401 out of the error toast stream while other failures still report', () => {
    expect(requestFailureMessage({ code: 'http', status: 401 })).toBe('');
    expect(requestFailureMessage({ code: 'http', status: 403 })).not.toBe('');
    expect(requestFailureMessage({ code: 'http', status: 500 })).not.toBe('');
  });

  // Marketing and Editorial share this feedback module; each app bundle owns its own bus instance.
  it('publishes one neutral sign-out notice in the operator language', () => {
    const notify = vi.spyOn(feedbackBus, 'notify');
    useUiLocale().setUiLocale('ru');
    sessionExpiryNotice()();
    expect(notify).toHaveBeenCalledExactlyOnceWith(
      expect.objectContaining({
        id: 'session-expired',
        tone: NoticeTone.Info,
        title: 'Вы вышли из аккаунта — войдите снова',
      }),
    );
  });
});
