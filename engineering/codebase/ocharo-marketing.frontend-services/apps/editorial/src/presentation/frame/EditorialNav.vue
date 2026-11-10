<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { ExternalLink } from 'lucide-vue-next';
import { SidebarMenu, SidebarMenuItem } from '@wow-two-beta/fe/presentation/nav';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { EditorialSections, PublicSitePath, type EditorialSection } from '../../domain/shared/EditorialSections';
import { SectionIcons } from './ShellIcons';

/**
 * Editorial's sidebar: Stories, Topics and Site settings, then the public site in a new tab. As an icon rail the labels
 * stay for assistive technology and show as tooltips. The frame owns the collapse control.
 */
defineOptions({ name: 'EditorialNav' });
const props = defineProps<{ current: EditorialSection | null; collapsed: boolean }>();
const { t } = useUiLocale();
const labelClass = computed(() => (props.collapsed ? 'sr-only' : 'min-w-0 flex-1 truncate text-start'));
</script>

<template>
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
</template>
