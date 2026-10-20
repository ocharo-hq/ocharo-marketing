/** Marketing's places as the frame lists them: the sidebar, the icon rail, the phone bar and the breadcrumb. */
export const MarketingSectionGroup = {
  Create: 'create',
  Plan: 'plan',
  Results: 'results',
  Settings: 'settings',
} as const;

export type MarketingSectionGroup = (typeof MarketingSectionGroup)[keyof typeof MarketingSectionGroup];

/** Names one section of Marketing. */
export type MarketingSection =
  | 'today'
  | 'posts'
  | 'products'
  | 'atlas'
  | 'creative-imports'
  | 'calendar'
  | 'automation'
  | 'channels'
  | 'inquiries'
  | 'monitoring'
  | 'results'
  | 'connections';

/** One sidebar destination. */
export interface MarketingSectionEntry {
  readonly id: MarketingSection;
  /** The English source label; the frame translates it. */
  readonly label: string;
  readonly path: string;
  readonly group: MarketingSectionGroup;
  /** Whether the phone's bottom bar carries it; the rest open from More. */
  readonly onPhoneBar: boolean;
}

/** The sections in display order; the first four ride the phone's bottom bar. */
export const MarketingSections: readonly MarketingSectionEntry[] = [
  { id: 'today', label: 'Today', path: '/', group: MarketingSectionGroup.Create, onPhoneBar: true },
  { id: 'posts', label: 'Posts', path: '/posts', group: MarketingSectionGroup.Create, onPhoneBar: true },
  { id: 'products', label: 'Products', path: '/products', group: MarketingSectionGroup.Create, onPhoneBar: true },
  { id: 'atlas', label: 'Atlas', path: '/atlas', group: MarketingSectionGroup.Create, onPhoneBar: true },
  {
    id: 'creative-imports',
    label: 'Studio imports',
    path: '/creative-imports',
    group: MarketingSectionGroup.Create,
    onPhoneBar: false,
  },
  { id: 'calendar', label: 'Calendar', path: '/calendar', group: MarketingSectionGroup.Plan, onPhoneBar: false },
  { id: 'automation', label: 'Automation', path: '/automation', group: MarketingSectionGroup.Plan, onPhoneBar: false },
  { id: 'channels', label: 'Channels', path: '/channels', group: MarketingSectionGroup.Plan, onPhoneBar: false },
  { id: 'inquiries', label: 'Enquiries', path: '/inquiries', group: MarketingSectionGroup.Results, onPhoneBar: false },
  {
    id: 'monitoring',
    label: 'Monitoring',
    path: '/monitoring',
    group: MarketingSectionGroup.Results,
    onPhoneBar: false,
  },
  { id: 'results', label: 'Results', path: '/results', group: MarketingSectionGroup.Results, onPhoneBar: false },
  {
    id: 'connections',
    label: 'Connections',
    path: '/connections',
    group: MarketingSectionGroup.Settings,
    onPhoneBar: false,
  },
];

/** The heading each group carries in the sidebar and the breadcrumb. */
export const MarketingGroupLabels: Readonly<Record<MarketingSectionGroup, string>> = {
  create: 'Create',
  plan: 'Plan',
  results: 'Results',
  settings: 'Settings',
};

/** Finds a section by its id. */
export function marketingSection(id: MarketingSection | null | undefined): MarketingSectionEntry | null {
  return MarketingSections.find((section) => section.id === id) ?? null;
}

/** Reads a route's section from its metadata; a place outside every section yields `null`. */
export function readMarketingSection(meta: Readonly<Record<string, unknown>>): MarketingSection | null {
  const value = meta.section;
  return MarketingSections.some((section) => section.id === value) ? (value as MarketingSection) : null;
}
