<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink } from 'vue-router';
import { Ellipsis, ExternalLink, X } from 'lucide-vue-next';
import { BottomNavMenu, BottomNavMenuItem } from '@wow-two-beta/fe/presentation/nav';
import { Modal, ModalContent, ModalTitle } from '@wow-two-beta/fe/presentation/overlays';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { EditorialSections, PublicSitePath, type EditorialSection } from '../../domain/shared/EditorialSections';
import { SectionIcons } from './ShellIcons';

/**
 * The phone's navigation: a bottom bar of Editorial's sections, then More, a full-screen dialog holding the public
 * site link. The bar sits in the shell's footer row, so the scrolling region ends above it.
 */
defineOptions({ name: 'PhoneNav' });
const props = defineProps<{ current: EditorialSection | null }>();
const { t } = useUiLocale();
const moreOpen = ref(false);
</script>

<template>
  <BottomNavMenu
    :is-fixed="false"
    v-bind="{ 'aria-label': t('Editorial sections') }"
    class="no-print oc-glass z-sticky border-t border-(--oc-glass-edge) pb-[env(safe-area-inset-bottom)] [grid-area:footer] md:hidden"
  >
    <BottomNavMenuItem
      v-for="section in EditorialSections"
      :key="section.id"
      as-child
      :is-active="props.current === section.id"
    >
      <template #icon><component :is="SectionIcons[section.id]" :size="20" /></template>
      <RouterLink :to="section.path">{{ t(section.label) }}</RouterLink>
    </BottomNavMenuItem>
    <BottomNavMenuItem as-child>
      <template #icon><Ellipsis :size="20" /></template>
      <button type="button" aria-haspopup="dialog" :aria-expanded="moreOpen" @click="moreOpen = true">
        {{ t('More') }}
      </button>
    </BottomNavMenuItem>
  </BottomNavMenu>
  <Modal v-model:open="moreOpen">
    <ModalContent
      v-if="moreOpen"
      class="oc-glass-menu flex max-h-dvh flex-col gap-4 overflow-y-auto p-5 pt-[calc(1.25rem+env(safe-area-inset-top))] text-foreground sm:max-w-md sm:rounded-2xl"
    >
      <div class="flex items-center justify-between gap-3">
        <ModalTitle class="text-lg font-semibold">{{ t('More in Editorial') }}</ModalTitle>
        <button
          type="button"
          class="grid size-11 place-items-center rounded-xl text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          :aria-label="t('Close')"
          @click="moreOpen = false"
        >
          <X :size="20" aria-hidden="true" />
        </button>
      </div>
      <nav :aria-label="t('More in Editorial')">
        <ul role="list" class="flex flex-col gap-1">
          <li>
            <a
              :href="PublicSitePath"
              target="_blank"
              rel="noopener"
              class="flex min-h-12 items-center gap-3 rounded-xl px-3 text-sm font-medium transition-colors hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              @click="moreOpen = false"
            >
              <span class="oc-app-icon"><ExternalLink :size="18" /></span>{{ t('Open site') }}
              <span class="sr-only">{{ t('(opens in a new tab)') }}</span>
            </a>
          </li>
        </ul>
      </nav>
    </ModalContent>
  </Modal>
</template>
