/** Editorial's places as the frame lists them: the sidebar, the icon rail, the phone bar and the breadcrumb. */
export type EditorialSection = 'stories' | 'topics' | 'settings';

/** One sidebar destination. */
export interface EditorialSectionEntry {
  readonly id: EditorialSection;
  /** The English source label; the frame translates it. */
  readonly label: string;
  /** The path under the router's `/studio/` base. */
  readonly path: string;
}

/** The sections in display order; all three ride the phone's bottom bar. */
export const EditorialSections: readonly EditorialSectionEntry[] = [
  { id: 'stories', label: 'Stories', path: '/' },
  { id: 'topics', label: 'Topics', path: '/topics' },
  { id: 'settings', label: 'Site settings', path: '/settings' },
];

/** The public site the stories publish to, served by the same host at its root, outside the Studio. */
export const PublicSitePath = '/';

/** Finds a section by its id. */
export function editorialSection(id: EditorialSection | null | undefined): EditorialSectionEntry | null {
  return EditorialSections.find((section) => section.id === id) ?? null;
}

/** Reads a route's section from its metadata; a place outside every section yields `null`. */
export function readEditorialSection(meta: Readonly<Record<string, unknown>>): EditorialSection | null {
  const value = meta.section;
  return EditorialSections.some((section) => section.id === value) ? (value as EditorialSection) : null;
}
