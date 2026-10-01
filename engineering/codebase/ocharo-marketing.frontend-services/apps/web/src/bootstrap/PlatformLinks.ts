/** The Ocharo platform family; override per environment with VITE_OCHARO_* variables. */
export interface PlatformLink {
  key: 'brand' | 'catalogue' | 'marketing' | 'blog' | 'studio';
  label: string;
  hint: string;
  url: string;
}

const env = import.meta.env as Record<string, string | undefined>;

export const platformLinks: PlatformLink[] = [
  {
    key: 'brand',
    label: 'Brend',
    hint: 'Logotip, ranglar, havolalar',
    url: env.VITE_OCHARO_BRAND_URL ?? 'https://localhost:8264/',
  },
  {
    key: 'catalogue',
    label: 'Katalog',
    hint: 'Mahsulot, narx, rasm',
    url: env.VITE_OCHARO_CATALOGUE_URL ?? 'https://localhost:8280/catalogue/',
  },
  {
    key: 'marketing',
    label: 'Marketing',
    hint: 'Post va avtomatika',
    url: env.VITE_OCHARO_MARKETING_URL ?? 'https://localhost:8358/',
  },
  {
    key: 'blog',
    label: 'Blog',
    hint: 'Hikoyalar studiyasi',
    url: env.VITE_OCHARO_BLOG_URL ?? 'https://localhost:8284/studio/',
  },
  {
    key: 'studio',
    label: 'Studio',
    hint: '3D kiyim studiyasi',
    url: env.VITE_OCHARO_STUDIO_URL ?? 'https://localhost:8262/',
  },
];
