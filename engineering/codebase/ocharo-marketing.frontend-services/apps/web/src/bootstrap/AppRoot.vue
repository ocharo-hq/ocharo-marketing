<script setup lang="ts">
import { FeedbackToastHost } from '@wow-two-beta/ui-vue/presentation/feedback';
import { feedbackBus, useErrorToast } from '../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { provideLocale } from '@wow-two-beta/ui-vue/foundation/i18n';
import LanguagePicker from '@ocharomarketing/connect/LanguagePicker.vue';
import BuildVersion from '@ocharomarketing/connect/BuildVersion.vue';
const appVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : 'dev';
import ConnectDesk from '@ocharomarketing/connect/ConnectDesk.vue';
const connectPlatformUrl = import.meta.env.VITE_PLATFORM_URL ?? 'https://localhost:8264/';
import { watchEffect, computed, onMounted, onScopeDispose, watch } from 'vue';
import { RouterView, useRoute } from 'vue-router';
import {
  BookOpen,
  CalendarDays,
  ChevronRight,
  CirclePlus,
  Feather,
  FolderOpen,
  LayoutDashboard,
  Link2,
  RefreshCw,
  TrendingUp,
  X,
  Zap,
} from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { AppErrorBoundary, RouteAnnouncer } from '@wow-two-beta/ui-vue/router';
import { useMarketingWorkspace } from '../application/marketing';
import { platformLinks } from './PlatformLinks';
const { t: ui, uiLocale, sdkMessages } = useUiLocale();
provideLocale(uiLocale, sdkMessages);

/** Composes Maftuna’s local marketing desk with real persisted workspace state. */
defineOptions({ name: 'AppRoot' });
const desk = useMarketingWorkspace();
useErrorToast(desk.error);
const workspace = desk.workspace;
const route = useRoute();
const navigation = [
  { path: '/', label: 'Bugun', icon: LayoutDashboard },
  { path: '/posts', label: 'Postlar', icon: Feather },
  { path: '/products', label: 'Mahsulotlar', icon: FolderOpen },
  { path: '/atlas', label: 'Atlas', icon: BookOpen },
  { path: '/calendar', label: 'Taqvim', icon: CalendarDays },
  { path: '/automation', label: 'Avtomatika', icon: Zap },
  { path: '/creative-imports', label: 'Studio imports', icon: FolderOpen },
  { path: '/inquiries', label: 'Enquiries', icon: Link2 },
  { path: '/channels', label: 'Kanallar xaritasi', icon: Link2 },
  { path: '/monitoring', label: 'Monitoring', icon: TrendingUp },
  { path: '/results', label: 'Natijalar', icon: TrendingUp },
  { path: '/connections', label: 'Ulanishlar', icon: Link2 },
];
let timer: ReturnType<typeof setTimeout> | undefined;
let disposed = false;
/** The active section supports direct loads and browser history. */
const section = computed(
  () =>
    navigation.find((item) => (item.path === '/' ? route.path === '/' : route.path.startsWith(item.path)))?.label ??
    'Ocharo',
);
watchEffect(
  () => {
    document.title = `${ui(section.value)} · Ocharo Marketing`;
  },
  { flush: 'post' },
);
/** Determines whether a recorded navigation item is active. */
function isActive(path: string) {
  return path === '/' ? route.path === '/' : route.path.startsWith(path);
}
/** Polls only pending jobs while preserving unsaved editor state. */
function schedulePoll() {
  if (timer) clearTimeout(timer);
  if (disposed || !desk.pendingJobs.value) return;
  timer = setTimeout(async () => {
    await desk.refresh(true);
    schedulePoll();
  }, 1600);
}
watch(desk.pendingJobs, schedulePoll);
onMounted(async () => {
  await desk.refresh();
  schedulePoll();
});
onScopeDispose(() => {
  disposed = true;
  if (timer) clearTimeout(timer);
});
</script>

<template>
  <div class="min-h-screen bg-background text-foreground">
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:fixed focus:left-5 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-card focus:px-5 focus:py-3"
      >{{ ui('Skip to main content') }}</a
    >
    <aside
      class="fixed inset-y-0 left-0 z-20 hidden w-60 flex-col overflow-y-auto border-r border-border bg-card lg:flex"
    >
      <div class="px-7 pb-9 pt-9">
        <p class="font-editorial text-4xl tracking-tight">Ocharo<span class="text-primary">.</span></p>
        <p class="mt-2 text-[10px] uppercase tracking-[0.25em] text-muted-foreground">{{ ui('Marketing desk') }}</p>
      </div>
      <nav :aria-label="ui('Main sections')" class="space-y-1 px-4">
        <button
          v-for="item in navigation"
          :key="item.path"
          :class="[
            'flex w-full items-center gap-3 rounded-xl px-4 py-3 text-left text-sm transition-colors',
            isActive(item.path)
              ? 'bg-primary-soft font-semibold text-primary'
              : 'text-muted-foreground hover:bg-muted hover:text-foreground',
          ]"
          :aria-current="isActive(item.path) ? 'page' : undefined"
          @click="desk.navigate(item.path)"
        >
          <Icon :icon="item.icon" :size="18" />{{ ui(item.label)
          }}<span
            v-if="item.path === '/posts' && workspace?.drafts.length"
            class="ml-auto rounded-full bg-card/80 px-2 py-0.5 text-[10px]"
            >{{ workspace.drafts.length }}</span
          >
        </button>
      </nav>
      <nav :aria-label="ui('Ocharo platform')" class="mx-5 mt-auto mb-3 text-[11px]">
        <p class="mb-1 px-2 font-semibold uppercase tracking-[0.2em] text-muted-foreground">Ocharo</p>
        <a
          v-for="link in platformLinks.filter((item) => item.key !== 'marketing')"
          :key="link.key"
          :href="link.url"
          class="block rounded-lg px-2 py-1 text-muted-foreground hover:bg-muted hover:text-foreground"
          ><span class="font-semibold">{{ ui(link.label) }}</span> · {{ ui(link.hint) }}</a
        >
      </nav>
      <div class="mx-5 mb-5 rounded-xl border border-border bg-background p-4">
        <div class="flex items-center gap-3">
          <span
            class="flex h-9 w-9 items-center justify-center rounded-full bg-primary-soft font-editorial text-lg text-primary"
            >M</span
          >
          <div>
            <p class="text-xs font-semibold">Maftuna</p>
            <p class="mt-1 text-[10px] text-muted-foreground">{{ workspace?.brand.handle ?? 'Ocharo' }}</p>
          </div>
        </div>
        <p class="mt-4 text-[10px] leading-5 text-muted-foreground">{{ ui('Local workspace · Tashkent') }}</p>
      </div>
    </aside>
    <div class="lg:ml-60">
      <header
        class="sticky top-0 z-10 flex min-h-20 flex-wrap items-center justify-between py-3 gap-4 border-b border-border bg-background/95 px-5 backdrop-blur-sm sm:px-8 xl:px-10"
      >
        <div class="flex items-center gap-3">
          <span class="font-editorial text-2xl lg:hidden">Ocharo.</span
          ><span class="hidden text-xs text-muted-foreground sm:inline">{{ ui('Desk') }}</span
          ><BuildVersion :app-version="appVersion" service-label="Marketing" /><Icon
            :icon="ChevronRight"
            :size="13"
            class="hidden text-muted-foreground sm:block"
          /><span class="text-xs font-semibold">{{ ui(section) }}</span>
        </div>
        <div class="flex flex-wrap items-center gap-3">
          <LanguagePicker />
          <Button
            variant="ghost"
            size="sm"
            :aria-label="ui('Refresh status')"
            :is-disabled="desk.busy.value !== null"
            @click="desk.refresh(true)"
            ><Icon :icon="RefreshCw" :size="16" /></Button
          ><Button size="sm" @click="desk.navigate('/posts/new')"
            ><Icon :icon="CirclePlus" :size="16" class="mr-2" />{{ ui('New post') }}</Button
          >
        </div>
      </header>
      <nav
        :aria-label="ui('Mobile sections')"
        class="flex gap-2 overflow-x-auto border-b border-border px-5 py-3 lg:hidden"
      >
        <button
          v-for="item in navigation"
          :key="item.path"
          :class="[
            'flex shrink-0 items-center gap-2 rounded-lg px-3 py-2 text-xs',
            isActive(item.path) ? 'bg-primary-soft text-primary' : 'text-muted-foreground',
          ]"
          :aria-current="isActive(item.path) ? 'page' : undefined"
          @click="desk.navigate(item.path)"
        >
          <Icon :icon="item.icon" :size="14" />{{ ui(item.label) }}
        </button>
      </nav>
      <main id="main-content" tabindex="-1" class="mx-auto max-w-[1600px] px-5 py-8 sm:px-8 xl:px-10">
        <ConnectDesk :platform-url="connectPlatformUrl" />
        <div
          v-if="desk.notice.value"
          role="status"
          class="mb-6 flex items-start justify-between gap-4 rounded-xl border border-success/20 bg-success-soft px-5 py-4 text-sm text-success-soft-foreground"
        >
          <p class="leading-6">{{ ui(desk.notice.value) }}</p>
          <button class="shrink-0 rounded p-1" :aria-label="ui('Dismiss message')" @click="desk.notice.value = null">
            <Icon :icon="X" :size="15" />
          </button>
        </div>
        <div
          v-if="desk.loading.value"
          class="grid min-h-96 gap-5 md:grid-cols-3"
          aria-busy="true"
          :aria-label="ui('Loading workspace…')"
        >
          <div v-for="card in 3" :key="card" class="animate-pulse rounded-2xl border border-border bg-card p-6">
            <div class="h-5 w-2/3 rounded bg-muted" />
            <div class="mt-6 h-36 rounded bg-muted" />
            <div class="mt-5 h-4 w-1/2 rounded bg-muted" />
          </div>
        </div>
        <section
          v-if="!workspace && !desk.loading.value"
          class="rounded-2xl border border-border bg-card p-12 text-center"
        >
          <h1 class="font-editorial text-3xl">{{ ui('Unable to connect to the workspace.') }}</h1>
          <p class="mt-4 text-sm text-muted-foreground">{{ ui('Your data is preserved. Try again.') }}</p>
          <Button class="mt-6" @click="desk.refresh()">{{ ui('Retry') }}</Button>
        </section>
        <AppErrorBoundary v-if="workspace" v-show="!desk.loading.value"
          ><RouterView v-slot="{ Component }"><component :is="Component" :key="route.fullPath" /></RouterView
          ><template #fallback="{ reset }"
            ><section class="rounded-2xl border border-border bg-card p-10 text-center">
              <h1 class="font-editorial text-2xl">{{ ui('Unable to open this section.') }}</h1>
              <p class="mt-4 text-sm text-muted-foreground">{{ ui('Your data is preserved. Try again.') }}</p>
              <Button class="mt-5" @click="reset">{{ ui('Retry') }}</Button>
            </section></template
          ></AppErrorBoundary
        >
      </main>
      <FeedbackToastHost :bus="feedbackBus" position="top-right" />
      <RouteAnnouncer />
    </div>
  </div>
</template>
