<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import { ChevronRight, CirclePlus, ExternalLink, X } from 'lucide-vue-next';
import { NoticeTone } from '@wow-two-beta/fe/feedback';
import { TailwindBreakpoints, useBreakpoint } from '@wow-two-beta/fe/foundation/device';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { localStorageStorageBroker, usePersistentState } from '@wow-two-beta/fe/foundation/storage';
import {
  AppShell,
  AppShellContent,
  AppShellHeader,
  AppShellMain,
  AppShellSidebar,
} from '@wow-two-beta/fe/presentation/layout';
import { AccountMenu, AppSwitcherMenu, MenuItem, type AccountMenuVersion } from '@wow-two-beta/fe/presentation/nav';
import { AppErrorBoundary } from '@wow-two-beta/fe/router';
import {
  integrationAccess,
  integrationAccount,
  integrationMode,
  integrationSession,
  integrationSigningOut,
  signOutIntegration,
} from '@ocharomarketing/connect';
import ConnectDesk from '@ocharomarketing/connect/ConnectDesk.vue';
import { notifyFeedback, useErrorToast } from '@ocharomarketing/connect/Feedback';
import { useOcharoAppSwitcherItems, usePlatformUrl } from '@ocharomarketing/connect/OcharoAppsConfig';
import { useServiceVersion } from '@ocharomarketing/connect/ServiceVersion';
import { unsavedWork } from '@ocharomarketing/connect/UnsavedWork';
import { interfaceLanguages, useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { useBlogStore } from '../application/blog';
import { editorialSection, readEditorialSection } from '../domain/shared/EditorialSections';
import EditorialNav from '../presentation/frame/EditorialNav.vue';
import PhoneNav from '../presentation/frame/PhoneNav.vue';

/** The sidebar's widths: full, and the icon rail. @internal */
const SidebarWidth = { Full: '244px', Rail: '68px' } as const;
/** Where this browser remembers the desktop sidebar's rail choice. @internal */
const SidebarCollapsedKey = 'ocharo.editorial.sidebar-collapsed';
/** The message a held sign-out shows. @internal */
const SignOutHeldMessage = 'Save or discard your unsaved changes before signing out.';

/**
 * The Ocharo frame, as Editorial wears it, around every Studio place under `/studio/`. The glass top bar is the
 * suite's: the app button with the app version, where you are, New story and the account menu last. The sidebar is
 * Editorial's sections and the public site; it collapses to an icon rail on desktops and is always the rail on
 * tablets. Phones swap it for a bottom bar. Only the main region scrolls. Mounted editors stay inside a fieldset that
 * disables them while signed out, so an expired session never discards their edits.
 */
defineOptions({ name: 'AppLayout' });
const { t, uiLocale, setUiLocale } = useUiLocale();
const route = useRoute();
const store = useBlogStore();
useErrorToast(store.error);
const apps = useOcharoAppSwitcherItems();
const platformUrl = usePlatformUrl();
const { version: apiVersion, refresh: refreshApiVersion } = useServiceVersion();
const appVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : 'dev';
// The stored choice seeds the first render too, so a collapsed rail never animates in from the full sidebar.
const collapsed = usePersistentState(
  SidebarCollapsedKey,
  localStorageStorageBroker.read<boolean>(SidebarCollapsedKey) === true,
);
const breakpoint = useBreakpoint(TailwindBreakpoints);

const isDesktop = computed(() => ['lg', 'xl', '2xl'].includes(breakpoint.value ?? ''));
const isRail = computed(() => !isDesktop.value || collapsed.value.value);
const sidebarWidth = computed(() => (isRail.value ? SidebarWidth.Rail : SidebarWidth.Full));
const section = computed(() => readEditorialSection(route.meta));
/** The section and the place itself, each once: the place's label comes from its route metadata. */
const crumbs = computed(() => {
  const entry = editorialSection(section.value);
  const place = typeof route.meta.label === 'string' ? route.meta.label : null;
  const labels = [entry?.label ?? null, place].filter((label): label is string => Boolean(label));
  return labels.filter((label, index) => labels.indexOf(label) === index).map((label) => t(label));
});

/** Who is signed in, the account row and sign-out exist only with an Ocharo session; the local pilot has none. */
const signedIn = computed(() => integrationMode.value === 'sso' && integrationSession.value?.active === true);
const accountUrl = computed(() => (signedIn.value ? (integrationAccount.value?.accountUrl ?? null) : null));
const accountOpen = ref(false);
const accountIdentity = computed(() =>
  signedIn.value ? { description: t('Signed in through Ocharo Desk'), onSignOut: signOut } : {},
);
/** The app's own version, then the API's from its status, with the commit the build carries split off. */
const versions = computed<AccountMenuVersion[]>(() => {
  const [api = '', commit] = apiVersion.value.split('+');
  return [
    { label: t('Editorial'), version: appVersion },
    { label: 'API', version: t(api), ...(commit ? { commit } : {}) },
  ];
});
watch(accountOpen, (open) => {
  if (open) void refreshApiVersion();
});
/** Opens Desk's account page in another tab, so unsaved edits here stay. */
function openAccount(): void {
  if (accountUrl.value) window.open(accountUrl.value, '_blank', 'noopener,noreferrer');
}
/** Sign-out waits while the story editor's guard holds unsaved, pending-image or saving work. */
async function signOut(): Promise<void> {
  if (unsavedWork.value) {
    notifyFeedback(SignOutHeldMessage, 'sso:sign-out-held');
    return;
  }
  const result = await signOutIntegration();
  if (!result.ok && result.message) notifyFeedback(result.message, 'sso:sign-out', NoticeTone.Danger);
}

/** The places mount once the first sign-in is current, and stay mounted through later expiries. */
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
  <AppShell
    class="surface-ambient transition-[grid-template-columns] duration-200 ease-out motion-reduce:transition-none"
    sidebar-breakpoint="md"
    :sidebar-width="sidebarWidth"
  >
    <AppShellHeader
      height="auto"
      class="no-print oc-glass border-b border-(--oc-glass-edge) p-0 pt-[env(safe-area-inset-top)] shadow-(--oc-glass-shadow)"
    >
      <div class="flex min-h-15 w-full min-w-0 items-center gap-2 px-3 sm:gap-3 sm:px-4">
        <div class="flex shrink-0 items-center gap-2">
          <AppSwitcherMenu
            :items="apps"
            current="editorial"
            :label="t('Editorial')"
            subtitle="Ocharo"
            :title="t('Ocharo apps')"
            :sites-label="t('Public sites')"
            :close-label="t('Close')"
            :new-tab-label="t('(opens in a new tab)')"
            :trigger-label="t('Ocharo apps — {app}')"
            panel-class="oc-glass-menu border-(--oc-glass-edge) text-foreground shadow-(--oc-menu-shadow)"
            class="max-sm:gap-1.5 max-sm:[&>span:not([data-app-switcher-mark])]:sr-only [&[data-state=open]_[data-app-switcher-chevron]]:text-primary-soft-foreground"
          >
            <template #mark><span class="oc-mark">O</span></template>
          </AppSwitcherMenu>
          <span class="font-mono text-[11px] text-muted-foreground" :title="t('App version')">{{ appVersion }}</span>
        </div>
        <nav
          :aria-label="t('You are here')"
          class="hidden min-w-0 items-center gap-1.5 border-s border-border ps-3 text-sm text-muted-foreground md:flex"
        >
          <template v-for="(crumb, index) in crumbs" :key="index">
            <ChevronRight v-if="index > 0" :size="14" class="shrink-0" aria-hidden="true" />
            <span
              :class="index === crumbs.length - 1 ? 'truncate font-semibold text-foreground' : 'shrink-0'"
              :aria-current="index === crumbs.length - 1 ? 'page' : undefined"
              >{{ crumb }}</span
            >
          </template>
        </nav>
        <span class="flex-1" />
        <RouterLink v-if="integrationAccess" to="/stories/new" class="btn-primary"
          ><Icon :icon="CirclePlus" :size="16" /><span class="max-sm:sr-only">{{ t('New story') }}</span></RouterLink
        >
        <AccountMenu
          v-model:open="accountOpen"
          v-bind="accountIdentity"
          :locales="interfaceLanguages"
          :locale="uiLocale"
          :versions="versions"
          :is-busy="integrationSigningOut"
          panel-class="oc-glass-menu overflow-x-hidden border-(--oc-glass-edge) text-foreground shadow-(--oc-menu-shadow)"
          @update:locale="setUiLocale"
        >
          <template v-if="accountUrl" #items>
            <MenuItem @select="openAccount"
              ><ExternalLink :size="15" class="shrink-0" aria-hidden="true" />{{ t('Ocharo account') }}</MenuItem
            >
          </template>
        </AccountMenu>
      </div>
    </AppShellHeader>
    <AppShellSidebar class="no-print oc-glass-side border-e border-(--oc-glass-edge)">
      <EditorialNav
        :current="section"
        :collapsed="isRail"
        :can-toggle="isDesktop"
        @toggle="collapsed.setValue((previous) => !previous)"
      />
    </AppShellSidebar>
    <AppShellMain class="min-w-0">
      <AppShellContent gutter="lg" :max-width="1600" class="pb-12 pt-6 xl:px-10">
        <ConnectDesk :platform-url="platformUrl" />
        <div
          v-if="store.notice.value"
          role="status"
          class="mb-6 flex items-start justify-between gap-4 rounded-xl border border-success/20 bg-success-soft px-5 py-4 text-sm text-success-soft-foreground"
        >
          <p class="leading-6">{{ t(store.notice.value) }}</p>
          <button class="shrink-0 rounded p-1" :aria-label="t('Dismiss message')" @click="store.notice.value = null">
            <Icon :icon="X" :size="15" />
          </button>
        </div>
        <fieldset v-if="opened" :disabled="!integrationAccess" class="m-0 min-w-0 border-0 p-0">
          <AppErrorBoundary>
            <RouterView v-slot="{ Component }"><component :is="Component" :key="route.fullPath" /></RouterView>
            <template #fallback="{ reset }">
              <section class="panel p-10 text-center">
                <h1 class="font-editorial text-2xl">{{ t('Unable to open this section.') }}</h1>
                <button class="btn-primary mt-5" @click="reset">{{ t('Retry') }}</button>
              </section>
            </template>
          </AppErrorBoundary>
        </fieldset>
      </AppShellContent>
    </AppShellMain>
    <PhoneNav :current="section" />
  </AppShell>
</template>
