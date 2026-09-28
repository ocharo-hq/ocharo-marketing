/** The Ocharo platform family; override per environment with VITE_OCHARO_* variables. */
export interface PlatformLink {
  key: 'catalogue' | 'marketing' | 'blog' | 'studio';
  label: string;
  hint: string;
  url: string;
}

const env = import.meta.env as Record<string, string | undefined>;

export const platformLinks: PlatformLink[] = [
  {
    key: 'catalogue',
    label: 'Katalog',
    hint: 'Mahsulot, narx, rasm',
    url: env.VITE_OCHARO_CATALOGUE_URL ?? 'http://localhost:8279/',
  },
  {
    key: 'marketing',
    label: 'Marketing',
    hint: 'Post va avtomatika',
    url: env.VITE_OCHARO_MARKETING_URL ?? 'http://localhost:8277/',
  },
  {
    key: 'blog',
    label: 'Blog',
    hint: 'Hikoyalar studiyasi',
    url: env.VITE_OCHARO_BLOG_URL ?? 'http://localhost:8281/studio/',
  },
  {
    key: 'studio',
    label: 'Studio',
    hint: '3D kiyim studiyasi',
    url: env.VITE_OCHARO_STUDIO_URL ?? 'https://localhost:8262/',
  },
];
