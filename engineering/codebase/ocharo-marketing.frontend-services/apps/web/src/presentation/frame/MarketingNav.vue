<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { SidebarMenu, SidebarMenuItem, SidebarMenuSection } from '@wow-two-beta/fe/presentation/nav';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import {
  MarketingGroupLabels,
  MarketingSectionGroup,
  MarketingSections,
  type MarketingSection,
} from '../../domain/shared/MarketingSections';
import { SectionIcons } from './ShellIcons';

/**
 * Marketing's sidebar: Create, Plan, Results and Settings, each under its heading. As an icon rail the labels stay for
 * assistive technology and show as tooltips; the draft count hides there. The frame owns the collapse control.
 */
defineOptions({ name: 'MarketingNav' });
const props = withDefaults(
  defineProps<{ current: MarketingSection | null; collapsed: boolean; draftCount?: number }>(),
  {
    draftCount: 0,
  },
);
const { t } = useUiLocale();
const groups = computed(() =>
  Object.values(MarketingSectionGroup).map((group) => ({
    group,
    label: MarketingGroupLabels[group],
    sections: MarketingSections.filter((section) => section.group === group),
  })),
);
const labelClass = computed(() => (props.collapsed ? 'sr-only' : 'min-w-0 flex-1 truncate text-start'));
</script>

<template>
  <SidebarMenu
    v-bind="{ 'aria-label': t('Marketing sections') }"
    :is-collapsed="props.collapsed"
    class="min-h-0 flex-1 overflow-y-auto overflow-x-hidden py-3 [scrollbar-width:thin]"
  >
    <SidebarMenuSection
      v-for="(entry, index) in groups"
      :key="entry.group"
      :label="t(entry.label)"
      :class="index > 0 ? 'mt-3' : undefined"
    >
      <SidebarMenuItem
        v-for="section in entry.sections"
        :key="section.id"
        as-child
        :is-active="props.current === section.id"
      >
        <template #icon><component :is="SectionIcons[section.id]" :size="17" /></template>
        <RouterLink :to="section.path" :title="props.collapsed ? t(section.label) : undefined"
          ><span :class="labelClass">{{ t(section.label) }}</span></RouterLink
        >
        <template v-if="section.id === 'posts' && props.draftCount > 0" #trailing
          ><span
            class="rounded-full bg-muted px-2 py-0.5 text-[10px] font-semibold tabular-nums text-muted-foreground"
            >{{ props.draftCount }}</span
          ></template
        >
      </SidebarMenuItem>
    </SidebarMenuSection>
  </SidebarMenu>
</template>
