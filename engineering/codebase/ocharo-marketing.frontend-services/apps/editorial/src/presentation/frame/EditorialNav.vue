<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { ExternalLink, PanelLeftClose, PanelLeftOpen } from 'lucide-vue-next';
import { SidebarMenu, SidebarMenuItem } from '@wow-two-beta/fe/presentation/nav';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { EditorialSections, PublicSitePath, type EditorialSection } from '../../domain/shared/EditorialSections';
import { SectionIcons } from './ShellIcons';

/**
 * Editorial's sidebar: Stories, Topics and Site settings, then the public site in a new tab, with the collapse control
 * last. As an icon rail the labels stay for assistive technology and show as tooltips.
 */
defineOptions({ name: 'EditorialNav' });
const props = withDefaults(
  defineProps<{ current: EditorialSection | null; collapsed: boolean; canToggle?: boolean }>(),
  {
    canToggle: true,
  },
);
const emit = defineEmits<{ toggle: [] }>();
const { t } = useUiLocale();
const labelClass = computed(() => (props.collapsed ? 'sr-only' : 'min-w-0 flex-1 truncate text-start'));
const toggleLabel = computed(() => t(props.collapsed ? 'Expand sidebar' : 'Collapse sidebar'));
</script>

<template>
  <div class="flex h-full min-h-0 flex-col">
    <SidebarMenu
      v-bind="{ 'aria-label': t('Editorial sections') }"
      :is-collapsed="props.collapsed"
      class="min-h-0 flex-1 overflow-y-auto overflow-x-hidden py-3 [scrollbar-width:thin]"
    >
      <SidebarMenuItem
        v-for="section in EditorialSections"
        :key="section.id"
        as-child
        :is-active="props.current === section.id"
      >
        <template #icon><component :is="SectionIcons[section.id]" :size="17" /></template>
        <RouterLink :to="section.path" :title="props.collapsed ? t(section.label) : undefined"
          ><span :class="labelClass">{{ t(section.label) }}</span></RouterLink
        >
      </SidebarMenuItem>
      <SidebarMenuItem as-child class="mt-3">
        <template #icon><ExternalLink :size="17" /></template>
        <a :href="PublicSitePath" target="_blank" rel="noopener" :title="props.collapsed ? t('Open site') : undefined"
          ><span :class="labelClass"
            >{{ t('Open site') }} <span class="sr-only">{{ t('(opens in a new tab)') }}</span></span
          ></a
        >
      </SidebarMenuItem>
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
