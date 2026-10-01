import type { EngagementObservationModel } from './models/OperationsModels';

/** Human labels and same-source comparisons for operator-entered evidence. */
export const OperationsExtensions = {
  channels: {
    instagram: 'Instagram',
    telegram: 'Telegram',
    x: 'X',
    whatsapp: 'WhatsApp',
    blog: 'Ocharo Blog',
    atlas: 'Atlas',
    wear: 'Wear',
    catalogue: 'Catalogue',
  } as Record<string, string>,
  metrics: [
    { key: 'views', label: 'Ko‘rishlar' },
    { key: 'reach', label: 'Qamrov' },
    { key: 'likes', label: 'Yoqtirishlar' },
    { key: 'comments', label: 'Izohlar' },
    { key: 'shares', label: 'Ulashishlar' },
    { key: 'saves', label: 'Saqlashlar' },
    { key: 'clicks', label: 'Havola bosishlar' },
    { key: 'enquiries', label: 'So‘rovlar' },
    { key: 'orders', label: 'Buyurtmalar' },
  ] as const,
  latest(values: EngagementObservationModel[]) {
    const sorted = [...values].sort(
      (a, b) =>
        b.observation.observedAt.localeCompare(a.observation.observedAt) || b.recordedAt.localeCompare(a.recordedAt),
    );
    const keys = new Set<string>();
    return sorted.filter((item) => {
      const key = JSON.stringify([item.observation.channel, item.observation.postUrl]);
      if (keys.has(key)) return false;
      keys.add(key);
      return true;
    });
  },
  previous(current: EngagementObservationModel, values: EngagementObservationModel[]) {
    return [...values]
      .filter(
        (item) =>
          item.observation.channel === current.observation.channel &&
          item.observation.postUrl === current.observation.postUrl &&
          item.observation.observedAt < current.observation.observedAt,
      )
      .sort((a, b) => b.observation.observedAt.localeCompare(a.observation.observedAt))[0];
  },
  deliveryMode(channel: string) {
    if (channel === 'telegram' || channel === 'x') return 'Tasdiqlangan ulanish orqali yuborish yoki qo‘lda eksport';
    if (channel === 'instagram' || channel === 'whatsapp')
      return 'Qo‘lda joylash; tasdiqlangan avtomatik ulanish mavjud emas';
    return 'Ocharo tahririy ilovasi; ijtimoiy tarmoq yuborishidan alohida';
  },
};
