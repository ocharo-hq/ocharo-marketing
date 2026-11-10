<script setup lang="ts">
import { computed, onMounted, onScopeDispose, ref, watch } from 'vue';
import { RouterView, useRoute } from 'vue-router';
import { CirclePlus, RefreshCw, X } from 'lucide-vue-next';
import { NoticeTone } from '@wow-two-beta/fe/feedback';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { useRunningVersion } from '@wow-two-beta/fe/foundation/net';
import { Button } from '@wow-two-beta/fe/presentation/actions';
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

import { useMarketingWorkspace } from '../application/common';
import { MarketingGroupLabels, marketingSection, readMarketingSection } from '../domain/shared/MarketingSections';
import MarketingNav from '../presentation/frame/MarketingNav.vue';
import PhoneNav from '../presentation/frame/PhoneNav.vue';
import WorkspaceSkeleton from '../presentation/common/WorkspaceSkeleton.vue';

/** Where this browser remembers the desktop sidebar's rail choice; the key predates the shared frame. @internal */
const SidebarCollapsedKey = 'ocharo.marketing.sidebar-collapsed';
/** The message a held sign-out shows. @internal */
const SignOutHeldMessage = 'Save or discard your unsaved changes before signing out.';

/**
 * Marketing inside the frame every Ocharo app shares (the fe sdk's `AppFrame`): the bar, the sidebar and the page
 * column are the suite's, at one size in every app. Marketing passes what is its own: the app list, its sections,
 * where you are, Refresh and New post, the account, and its notices above the page. Mounted editors stay inside a
 * fieldset that disables them while signed out, so an expired session never discards their edits.
 */
defineOptions({ name: 'AppLayout' });
const { t, uiLocale, setUiLocale } = useUiLocale();
const route = useRoute();
const desk = useMarketingWorkspace();
useErrorToast(desk.error);
const workspace = desk.workspace;
const apps = useOcharoAppSwitcherItems();
const { version: apiVersion, refresh: refreshApiVersion } = useServiceVersion();
const appVersion = typeof __APP_VERSION__ === 'string' ? __APP_VERSION__ : 'dev';
// The badge beside the app button follows the release the API serves; a local build has no release to compare.
const { version: runningVersion } = useRunningVersion(() => (appVersion === 'dev' ? null : readRunningVersion()));

const section = computed(() => readMarketingSection(route.meta));
/** The app menu's own phrases. */
const switcher = computed<AppFrameSwitcher>(() => ({
  title: t('Ocharo apps'),
  sitesLabel: t('Public sites'),
  closeLabel: t('Close'),
  newTabLabel: t('(opens in a new tab)'),
  triggerLabel: t('Ocharo apps — {app}'),
}));
/** The group, the section and the place itself, each once: the place's label comes from its route metadata. */
const crumbs = computed(() => {
  const entry = marketingSection(section.value);
  const group = entry ? MarketingGroupLabels[entry.group] : null;
  const place = typeof route.meta.label === 'string' ? route.meta.label : null;
  const labels = [group, entry?.label ?? null, place].filter((label): label is string => Boolean(label));
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
    { label: t('Marketing'), version: appVersion },
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
/** Sign-out waits while an editor's guard holds unsaved or saving work, or any workspace write is in flight. */
const signOutHeld = computed(() => unsavedWork.value || desk.busy.value !== null);
async function signOut(): Promise<void> {
  if (signOutHeld.value) {
    notifyFeedback(SignOutHeldMessage, 'identity:sign-out-held');
    return;
  }
  const result = await signOutIntegration();
  if (!result.ok && result.message) notifyFeedback(result.message, 'identity:sign-out', NoticeTone.Danger);
}

let timer: ReturnType<typeof setTimeout> | undefined;
let disposed = false;
/** Polls only pending jobs while preserving unsaved editor state. */
function schedulePoll() {
  if (timer) clearTimeout(timer);
  if (disposed || !integrationAccess.value || !desk.pendingJobs.value) return;
  timer = setTimeout(async () => {
    await desk.refresh(true);
    schedulePoll();
  }, 1600);
}
watch(desk.pendingJobs, schedulePoll);
onMounted(() => {
  watch(
    integrationAccess,
    async (active) => {
      if (active && !workspace.value) await desk.refresh();
      schedulePoll();
    },
    { immediate: true },
  );
});
onScopeDispose(() => {
  disposed = true;
  if (timer) clearTimeout(timer);
});
</script>

<template>
  <AppFrame
    v-model:account-open="accountOpen"
    :locale="uiLocale"
    :version="appVersion"
    :running-version="runningVersion"
    :apps="apps"
    current-app="marketing"
    :app-label="t('Marketing')"
    app-subtitle="Ocharo"
    :app-switcher="switcher"
    :crumbs="crumbs"
    :trail-label="t('You are here')"
    :account="account"
    :collapse-storage-key="SidebarCollapsedKey"
    @update:locale="setUiLocale"
  >
    <template #brand><span class="oc-mark">O</span></template>
    <template #nav="{ isRail }">
      <MarketingNav :current="section" :collapsed="isRail" :draft-count="workspace?.drafts.length ?? 0" />
    </template>
    <template #bar-end>
      <Button
        variant="ghost"
        size="sm"
        :aria-label="t('Refresh status')"
        :is-disabled="!integrationAccess || desk.busy.value !== null || desk.loading.value"
        :is-loading="desk.loading.value"
        @click="desk.refresh()"
        ><Icon :icon="RefreshCw" :size="16" /><span class="max-lg:sr-only">{{ t('Refresh') }}</span></Button
      >
      <Button size="sm" :is-disabled="!integrationAccess" @click="desk.navigate('/posts/new')"
        ><Icon :icon="CirclePlus" :size="16" /><span class="max-sm:sr-only">{{ t('New post') }}</span></Button
      >
    </template>
    <template #phone-nav><PhoneNav :current="section" /></template>
    <template #notice>
      <SessionConnection />
      <div
        v-if="desk.notice.value"
        role="status"
        class="mb-6 flex items-start justify-between gap-4 rounded-xl border border-success/20 bg-success-soft px-5 py-4 text-sm text-success-soft-foreground"
      >
        <p class="leading-6">{{ t(desk.notice.value) }}</p>
        <button class="shrink-0 rounded p-1" :aria-label="t('Dismiss message')" @click="desk.notice.value = null">
          <Icon :icon="X" :size="15" />
        </button>
      </div>
    </template>
    <WorkspaceSkeleton v-if="desk.loading.value && !workspace" :path="route.path" />
    <p v-if="integrationAccess && !workspace && !desk.loading.value" class="query-label" role="status">
      {{ t('Unable to connect to the workspace.') }}
    </p>
    <fieldset v-if="workspace" :disabled="!integrationAccess" class="m-0 min-w-0 border-0 p-0">
      <AppErrorBoundary
        ><RouterView v-slot="{ Component }"><component :is="Component" :key="route.fullPath" /></RouterView
        ><template #fallback="{ reset }"
          ><section class="rounded-2xl border border-border bg-card p-10 text-center">
            <h1 class="font-editorial text-2xl">{{ t('Unable to open this section.') }}</h1>
            <p class="mt-4 text-sm text-muted-foreground">{{ t('Your data is preserved. Try again.') }}</p>
            <Button class="mt-5" @click="reset">{{ t('Retry') }}</Button>
          </section></template
        ></AppErrorBoundary
      >
    </fieldset>
  </AppFrame>
</template>
