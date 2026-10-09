<script setup lang="ts">
import { FeedbackToastHost } from '@wow-two-beta/ui-vue/presentation/feedback';
import { feedbackBus, useErrorToast } from '../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { provideLocale } from '@wow-two-beta/ui-vue/foundation/i18n';
import LanguagePicker from '@ocharomarketing/connect/LanguagePicker.vue';
import BuildVersion from '@ocharomarketing/connect/BuildVersion.vue';
const appVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : 'dev';
import ConnectDesk from '@ocharomarketing/connect/ConnectDesk.vue';
import { integrationAccess } from '@ocharomarketing/connect';
const connectPlatformUrl = import.meta.env.VITE_PLATFORM_URL ?? 'https://localhost:8264/';
import { watchEffect, computed, watch, ref } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import { ChevronRight, CirclePlus, ExternalLink, FileText, Globe, Layers, X } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { AppErrorBoundary, RouteAnnouncer } from '@wow-two-beta/ui-vue/router';
import { useBlogStore } from '../application/blog';
import { platformLinks } from './PlatformLinks';
const { t: ui, uiLocale, sdkMessages } = useUiLocale();
provideLocale(uiLocale, sdkMessages);

/** Composes Ocharo Blog Studio: stories, topics and site settings. */
defineOptions({ name: 'AppRoot' });
const store = useBlogStore();
useErrorToast(store.error);
const route = useRoute();
const navigation = [
  { path: '/', label: 'Hikoyalar', icon: FileText },
  { path: '/topics', label: 'Bo‘limlar', icon: Layers },
  { path: '/settings', label: 'Sayt', icon: Globe },
];
function isActive(path: string) {
  return path === '/' ? route.path === '/' || route.path.startsWith('/stories') : route.path.startsWith(path);
}
const section = computed(() => navigation.find((item) => isActive(item.path))?.label ?? 'Ocharo');
watchEffect(
  () => {
    document.title = `${ui(section.value)} · Ocharo Blog`;
  },
  { flush: 'post' },
);
const opened = ref(false);
watch(
  integrationAccess,
  (active) => {
    if (active && !opened.value) {
      opened.value = true;
      void store.loadTopics();
    }
  },
  { immediate: true },
);
</script>

<template>
  <div class="min-h-screen bg-background text-foreground">
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:fixed focus:left-5 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-card focus:px-5 focus:py-3"
      >{{ ui('Skip to main content') }}</a
    >
    <aside class="fixed inset-y-0 left-0 z-20 hidden w-60 flex-col border-r border-border bg-card lg:flex">
      <div class="px-7 pb-9 pt-9">
        <p class="font-editorial text-4xl tracking-tight">Ocharo<span class="text-primary">.</span></p>
        <p class="mt-2 text-[10px] uppercase tracking-[0.25em] text-muted-foreground">{{ ui('Blog studio') }}</p>
      </div>
      <nav :aria-label="ui('Main sections')" class="space-y-1 px-4">
        <RouterLink
          v-for="item in navigation"
          :key="item.path"
          :to="item.path"
          :class="[
            'flex w-full items-center gap-3 rounded-xl px-4 py-3 text-left text-sm transition-colors',
            isActive(item.path)
              ? 'bg-primary-soft font-semibold text-primary'
              : 'text-muted-foreground hover:bg-muted hover:text-foreground',
          ]"
          :aria-current="isActive(item.path) ? 'page' : undefined"
        >
          <Icon :icon="item.icon" :size="18" />{{ ui(item.label) }}
        </RouterLink>
        <a
          href="/"
          target="_blank"
          rel="noopener"
          class="flex w-full items-center gap-3 rounded-xl px-4 py-3 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
        >
          <Icon :icon="ExternalLink" :size="18" />{{ ui('Open site') }}
        </a>
      </nav>
      <nav
        :aria-label="ui('Ocharo platform')"
        class="mx-5 mb-5 mt-auto rounded-xl border border-border bg-background p-3 text-[11px]"
      >
        <p class="mb-2 flex items-center gap-2 px-1 font-semibold text-foreground">{{ ui('Ocharo platform') }}</p>
        <a
          v-for="link in platformLinks"
          :key="link.key"
          :href="link.url"
          :aria-current="link.key === 'blog' ? 'page' : undefined"
          :class="[
            'block rounded-lg px-2 py-1.5',
            link.key === 'blog'
              ? 'bg-primary-soft text-primary'
              : 'text-muted-foreground hover:bg-muted hover:text-foreground',
          ]"
        >
          <span class="font-semibold">{{ ui(link.label) }}</span> · {{ ui(link.hint) }}
        </a>
      </nav>
    </aside>
    <div class="lg:ml-60">
      <header
        class="sticky top-0 z-10 flex min-h-20 flex-wrap items-center justify-between py-3 gap-4 border-b border-border bg-background/95 px-5 backdrop-blur-sm sm:px-8 xl:px-10"
      >
        <div class="flex items-center gap-3">
          <span class="font-editorial text-2xl lg:hidden">Ocharo.</span>
          <span class="hidden text-xs text-muted-foreground sm:inline">{{ ui('Blog') }}</span>
          <BuildVersion :app-version="appVersion" service-label="Editorial" />
          <Icon :icon="ChevronRight" :size="13" class="hidden text-muted-foreground sm:block" />
          <span class="text-xs font-semibold">{{ ui(section) }}</span>
        </div>
        <LanguagePicker />
        <RouterLink v-if="integrationAccess" to="/stories/new" class="btn-primary"
          ><Icon :icon="CirclePlus" :size="16" />{{ ui('New story') }}</RouterLink
        >
      </header>
      <nav
        :aria-label="ui('Mobile sections')"
        class="flex gap-2 overflow-x-auto border-b border-border px-5 py-3 lg:hidden"
      >
        <RouterLink
          v-for="item in navigation"
          :key="item.path"
          :to="item.path"
          :class="[
            'flex shrink-0 items-center gap-2 rounded-lg px-3 py-2 text-xs',
            isActive(item.path) ? 'bg-primary-soft text-primary' : 'text-muted-foreground',
          ]"
        >
          <Icon :icon="item.icon" :size="14" />{{ ui(item.label) }}
        </RouterLink>
      </nav>
      <main id="main-content" tabindex="-1" class="mx-auto max-w-[1600px] px-5 py-8 sm:px-8 xl:px-10">
        <ConnectDesk :platform-url="connectPlatformUrl" />
        <div
          v-if="store.notice.value"
          role="status"
          class="mb-6 flex items-start justify-between gap-4 rounded-xl border border-success/20 bg-success-soft px-5 py-4 text-sm text-success-soft-foreground"
        >
          <p class="leading-6">{{ ui(store.notice.value) }}</p>
          <button class="shrink-0 rounded p-1" :aria-label="ui('Dismiss message')" @click="store.notice.value = null">
            <Icon :icon="X" :size="15" />
          </button>
        </div>
        <fieldset v-if="opened" :disabled="!integrationAccess" class="min-w-0 border-0 p-0 m-0">
          <AppErrorBoundary>
            <RouterView v-slot="{ Component }"><component :is="Component" :key="route.fullPath" /></RouterView>
            <template #fallback="{ reset }">
              <section class="panel p-10 text-center">
                <h1 class="font-editorial text-2xl">{{ ui('Unable to open this section.') }}</h1>
                <button class="btn-primary mt-5" @click="reset">{{ ui('Retry') }}</button>
              </section>
            </template>
          </AppErrorBoundary>
        </fieldset>
      </main>
      <FeedbackToastHost :bus="feedbackBus" position="top-right" />
      <RouteAnnouncer />
    </div>
  </div>
</template>
