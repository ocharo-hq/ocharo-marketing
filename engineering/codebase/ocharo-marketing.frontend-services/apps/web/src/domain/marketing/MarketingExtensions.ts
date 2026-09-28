import type { ProductModel } from './models/ProductModel';
import type { WorkspaceModel } from './models/WorkspaceModel';

/** @internal The localized display labels for recorded workflow states. */
const StatusLabels: Record<string, string> = {
  draft: 'Qoralama',
  approved: 'Tekshirilgan',
  planned: 'Rejada',
  queued: 'Navbatda',
  running: 'Bajarilmoqda',
  completed: 'Tayyor',
  published: 'Joylangan',
  failed: 'Xato',
  unknown: "Natija noma'lum",
  cancelled: 'Bekor qilingan',
  'not-configured': 'Ulanmagan',
  configured: 'Sozlangan',
  unsupported: "Qo'llanmaydi",
  unavailable: "Ma'lumot yo'q",
  'not-connected': 'Ulanmagan',
  'not-supported': "Qo'llanmaydi",
  pending: 'Tekshirish kerak',
  TBD: 'Tekshirish kerak',
  confirmed: 'Tasdiqlangan',
  verified: 'Tekshirilgan',
  stale: 'Qayta tekshirish kerak',
  'verification-failed': 'Tekshiruv o‘tmadi',
  partial: 'Qisman olingan',
  'not-verified': 'Tekshirilmagan',
  held: 'To‘xtatilgan',
  revoked: 'Ruxsat bekor qilingan',
  accepted: 'Kanal qabul qilgan',
  ready: 'Ko‘rib chiqishga tayyor',
  reviewed: 'Matn tekshirilgan',
  observed: 'Kuzatilgan',
  succeeded: 'Bajarilgan',
};

/** Provides total presentation operations over marketing data. */
export const MarketingExtensions = {
  statusLabel(value: string): string {
    return StatusLabels[value] ?? value;
  },
  presetName(id: string, fallback: string): string {
    return (
      (
        { 't02-bone': 'Yorug‘ fon', 't02-charcoal': 'Qora fon', 't01-fabric-right': 'Mato + mahsulot' } as Record<
          string,
          string
        >
      )[id] ?? fallback
    );
  },
  presetDescription(id: string, fallback: string): string {
    return (
      (
        {
          't02-bone': 'Mahsulotning old ko‘rinishi iliq och fonda.',
          't02-charcoal': 'Xuddi shu kompozitsiya to‘q fonda.',
          't01-fabric-right': 'Mos mato fotosi o‘ng yarmida, mahsulot uning ustida.',
        } as Record<string, string>
      )[id] ?? fallback
    );
  },
  productAsset(workspace: WorkspaceModel | null, product: ProductModel | undefined): string | null {
    if (!workspace || !product) return null;
    return workspace.assets.find((asset) => asset.id === product.heroAssetId)?.url ?? null;
  },
  formatDate(value: string, options: Intl.DateTimeFormatOptions = {}): string {
    const date = new Date(value);
    if (!Number.isFinite(date.getTime())) return 'Sana tekshirilishi kerak';
    const parts = new Intl.DateTimeFormat('en-GB', {
      timeZone: 'Asia/Tashkent',
      day: 'numeric',
      month: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hourCycle: 'h23',
    }).formatToParts(date);
    const part = (name: string) => parts.find((item) => item.type === name)?.value ?? '';
    const months = ['yan', 'fev', 'mar', 'apr', 'may', 'iyn', 'iyl', 'avg', 'sen', 'okt', 'noy', 'dek'];
    const settings = { day: 'numeric', month: 'short', ...options };
    const result = [
      settings.day ? part('day') : '',
      settings.month ? months[Number(part('month')) - 1] : '',
      settings.year ? part('year') : '',
    ]
      .filter(Boolean)
      .join(' ');
    return settings.hour ? `${result}${result ? ' · ' : ''}${part('hour')}:${part('minute')}` : result;
  },
  formatPrice(value: number | null, currency: string): string {
    return value === null ? 'Narx kiritilmagan' : `${new Intl.NumberFormat('uz-UZ').format(value)} ${currency}`;
  },
  buildCaption(product: ProductModel): string {
    const lines = [product.name];
    if (product.material) lines.push(`Mato: ${product.material}`);
    if (product.sizes.length) lines.push(`O'lchamlar: ${product.sizes.join(', ')}`);
    if (product.price !== null)
      lines.push(`1 dona: ${new Intl.NumberFormat('uz-UZ').format(product.price)} ${product.currency}`);
    return lines.join('\n');
  },
  toTashkentInstant(value: string): string | null {
    if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)) return null;
    const date = new Date(`${value}:00+05:00`);
    if (!Number.isFinite(date.getTime())) return null;
    const local = new Date(date.getTime() + 5 * 60 * 60 * 1000).toISOString().slice(0, 16);
    return local === value ? date.toISOString() : null;
  },
} as const;
