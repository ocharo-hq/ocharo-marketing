import type { Component } from 'vue';
import { FileText, Globe, Layers } from 'lucide-vue-next';

import type { EditorialSection } from '../../domain/shared/EditorialSections';

/** The icon beside each Editorial section in the sidebar, the rail and the phone's bottom bar. */
export const SectionIcons: Readonly<Record<EditorialSection, Component>> = {
  stories: FileText,
  topics: Layers,
  settings: Globe,
};
