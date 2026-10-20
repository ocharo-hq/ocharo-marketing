import type { Component } from 'vue';
import {
  Activity,
  BookOpen,
  CalendarDays,
  ChartColumn,
  Feather,
  FolderOpen,
  Import,
  Inbox,
  LayoutDashboard,
  Plug,
  Radio,
  Zap,
} from 'lucide-vue-next';

import type { MarketingSection } from '../../domain/shared/MarketingSections';

/** The icon beside each Marketing section in the sidebar, the rail and the phone's bottom bar. */
export const SectionIcons: Readonly<Record<MarketingSection, Component>> = {
  today: LayoutDashboard,
  posts: Feather,
  products: FolderOpen,
  atlas: BookOpen,
  'creative-imports': Import,
  calendar: CalendarDays,
  automation: Zap,
  channels: Radio,
  inquiries: Inbox,
  monitoring: Activity,
  results: ChartColumn,
  connections: Plug,
};
