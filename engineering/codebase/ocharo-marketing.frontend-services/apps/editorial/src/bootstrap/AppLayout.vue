<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink, RouterView, useRoute } from 'vue-router';
import { CirclePlus, X } from 'lucide-vue-next';
import { NoticeTone } from '@wow-two-beta/fe/feedback';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { useRunningVersion } from '@wow-two-beta/fe/foundation/net';
import { AppFrame, type AppFrameAccount, type AppFrameSwitcher } from '@wow-two-beta/fe/presentation/layout';
import { type AccountMenuVersion } from '@wow-two-beta/fe/presentation/nav';
import { AppErrorBoundary } from '@wow-two-beta/fe/router';
import {
  integrationAccess,
  integrationAccount,
  integrationSigningOut,
  signOutIntegration,
} from '@ocharomarketing/connect';
import SessionConnection from '@ocharomarketing/connect/SessionConnection.vue';
import { notifyFeedback, useErrorToast } from '@ocharomarketing/connect/Feedback';
import { useOcharoAppSwitcherItems } from '@ocharomarketing/connect/OcharoAppsConfig';
import { readRunningVersion, useServiceVersion } from '@ocharomarketing/connect/ServiceVersion';
import { unsavedWork } from '@ocharomarketing/connect/UnsavedWork';
import { interfaceLanguages, useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { useBlogStore } from '../application/blog';
import { editorialSection, readEditorialSection } from '../domain/shared/EditorialSections';
import EditorialNav from '../presentation/frame/EditorialNav.vue';
import PhoneNav from '../presentation/frame/PhoneNav.vue';

/** Where this browser remembers the desktop sidebar's rail choice; the key predates the shared frame. @internal */
const SidebarCollapsedKey = 'ocharo.editorial.sidebar-collapsed';
/** The message a held sign-out shows. @internal */
const SignOutHeldMessage = 'Save or discard your unsaved changes before signing out.';

/**
 * Editorial inside the frame every Ocharo app shares (the fe sdk's `AppFrame`): the bar, the sidebar and the page
 * column are the suite's, at one size in every app. Editorial passes what is its own: the app list, its sections and
 * the public site, where you are, New story, the account, and its notices above the page. Mounted editors stay inside
 * a fieldset that disables them while signed out, so an expired session never discards their edits.
 */
defineOptions({ name: 'AppLayout' });
const { t, uiLocale, setUiLocale } = useUiLocale();
const route = useRoute();
const store = useBlogStore();
useErrorToast(store.error);
const apps = useOcharoAppSwitcherItems();
const { version: apiVersion, refresh: refreshApiVersion } = useServiceVersion();
const appVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : 'dev';
// The badge beside the app button follows the release the API serves; a local build has no release to compare.
const { version: runningVersion } = useRunningVersion(() => (appVersion === 'dev' ? null : readRunningVersion()));

const section = computed(() => readEditorialSection(route.meta));
/** The app menu's own phrases. */
const switcher = computed<AppFrameSwitcher>(() => ({
  title: t('Ocharo apps'),
  sitesLabel: t('Public sites'),
  closeLabel: t('Close'),
  newTabLabel: t('(opens in a new tab)'),
  triggerLabel: t('Ocharo apps — {app}'),
}));
/** The section and the place itself, each once: the place's label comes from its route metadata. */
const crumbs = computed(() => {
  const entry = editorialSection(section.value);
  const place = typeof route.meta.label === 'string' ? route.meta.label : null;
  const labels = [entry?.label ?? null, place].filter((label): label is string => Boolean(label));
  return labels.filter((label, index) => labels.indexOf(label) === index).map((label) => t(label));
});

/** Who is signed in, and sign-out, exist only with an Ocharo session. */
const accountOpen = ref(false);
/** The person Identity names, their contact and picture; a session that names neither still says it is signed in. */
const accountIdentity = computed(() => {
  const account = integrationAccount.value;
  if (!integrationAccess.value || !account) return {};
  const { name, email, phoneNumber, picture } = account;
  const contact = email ?? phoneNumber;
  if (!name && !contact) return { description: t('Signed in to Ocharo'), onSignOut: signOut };
  return {
    ...(name ? { name } : {}),
    // A person without a display name is named by their contact; it is not repeated under itself.
    ...(contact && contact !== name ? { description: contact } : {}),
    ...(picture ? { avatarSrc: picture } : {}),
    onSignOut: signOut,
  };
});
/** The app's own version, then the API's from its status, with the commit the build carries split off. */
const versions = computed<AccountMenuVersion[]>(() => {
  const [api = '', commit] = apiVersion.value.split('+');
  return [
    { label: t('Apps menu: Editorial'), version: appVersion },
    { label: 'API', version: t(api), ...(commit ? { commit } : {}) },
  ];
});
/** The account menu: who is signed in, the versions, the languages, and sign-out while it runs. */
const account = computed<AppFrameAccount>(() => ({
  ...accountIdentity.value,
  locales: interfaceLanguages,
  versions: versions.value,
  isBusy: integrationSigningOut.value,
}));
watch(accountOpen, (open) => {
  if (open) void refreshApiVersion();
});
/** Sign-out waits while the story editor's guard holds unsaved, pending-image or saving work. */
async function signOut(): Promise<void> {
  if (unsavedWork.value) {
    notifyFeedback(SignOutHeldMessage, 'identity:sign-out-held');
    return;
  }
  const result = await signOutIntegration();
  if (!result.ok && result.message) notifyFeedback(result.message, 'identity:sign-out', NoticeTone.Danger);
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
  <AppFrame
    v-model:account-open="accountOpen"
    :locale="uiLocale"
    :version="appVersion"
    :running-version="runningVersion"
    :apps="apps"
    current-app="editorial"
    :app-label="t('Apps menu: Editorial')"
    app-subtitle="Ocharo"
    :app-switcher="switcher"
    :crumbs="crumbs"
    :trail-label="t('You are here')"
    :account="account"
    :collapse-storage-key="SidebarCollapsedKey"
    @update:locale="setUiLocale"
  >
    <template #brand><span class="oc-mark">O</span></template>
    <template #nav="{ isRail }"><EditorialNav :current="section" :collapsed="isRail" /></template>
    <template #bar-end>
      <RouterLink v-if="integrationAccess" to="/stories/new" class="btn-primary"
        ><Icon :icon="CirclePlus" :size="16" /><span class="max-sm:sr-only">{{ t('New story') }}</span></RouterLink
      >
    </template>
    <template #phone-nav><PhoneNav :current="section" /></template>
    <template #notice>
      <SessionConnection />
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
    </template>
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
  </AppFrame>
</template>
