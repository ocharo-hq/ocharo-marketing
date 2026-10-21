<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { PanelLeftClose, PanelLeftOpen } from 'lucide-vue-next';
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
 * Marketing's sidebar: Create, Plan, Results and Settings, each under its heading, with the collapse control last. As
 * an icon rail the labels stay for assistive technology and show as tooltips; the draft count hides there.
 */
defineOptions({ name: 'MarketingNav' });
const props = withDefaults(
  defineProps<{ current: MarketingSection | null; collapsed: boolean; canToggle?: boolean; draftCount?: number }>(),
  { canToggle: true, draftCount: 0 },
);
const emit = defineEmits<{ toggle: [] }>();
const { t } = useUiLocale();
const groups = computed(() =>
  Object.values(MarketingSectionGroup).map((group) => ({
    group,
    label: MarketingGroupLabels[group],
    sections: MarketingSections.filter((section) => section.group === group),
  })),
);
const labelClass = computed(() => (props.collapsed ? 'sr-only' : 'min-w-0 flex-1 truncate text-start'));
const toggleLabel = computed(() => t(props.collapsed ? 'Expand sidebar' : 'Collapse sidebar'));
</script>

<template>
  <div class="flex h-full min-h-0 flex-col">
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
    <div v-if="props.canToggle" class="border-t border-glass-edge p-2">
      <button
        type="button"
        class="flex min-h-10 w-full items-center gap-2.5 rounded-lg px-2.5 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        :class="{ 'justify-center': props.collapsed }"
        :aria-label="toggleLabel"
        :aria-expanded="!props.collapsed"
        :title="props.collapsed ? toggleLabel : undefined"
        @click="emit('toggle')"
      >
        <component :is="props.collapsed ? PanelLeftOpen : PanelLeftClose" :size="17" aria-hidden="true" />
        <span v-if="!props.collapsed">{{ toggleLabel }}</span>
      </button>
    </div>
  </div>
</template>
