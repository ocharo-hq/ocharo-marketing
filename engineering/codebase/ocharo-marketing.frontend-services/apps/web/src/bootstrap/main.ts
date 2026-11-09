import './index.css';

import { createApp } from 'vue';
import { importMetaEnvSource } from '@wow-two-beta/fe/foundation/config';
import { configureIntegration, initializeIntegration, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { sessionExpiryNotice } from '@ocharomarketing/connect/Feedback';
import { loadOcharoApps, ocharoAppsPlugin, reportOcharoAppsStartup } from '@ocharomarketing/connect/OcharoAppsConfig';
import { initializeUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { createMarketingWorkspace, MarketingWorkspaceKey } from '../application/marketing';
import AppBoot from './AppBoot.vue';
import { createMarketingRouter } from './MarketingRouter';

initializeUiLocale();
// Identity grants this app `marketing:edit` and `marketing:approve`; the session's permissions are read under that name.
configureIntegration({ permissionNamespace: 'marketing' });
// An expired sign-in shows one neutral notice; the page and its unsaved edits stay while the operator signs in again.
subscribeIntegrationExpired(sessionExpiryNotice());
const router = createMarketingRouter();
const app = createApp(AppBoot);
const workspace = createMarketingWorkspace((path) => {
  void router.push(path);
});
app.provide(MarketingWorkspaceKey, workspace);
app.use(router);
// The app menu's runtime settings load beside the first route and the session, within a bounded wait; a failure falls
// back to the build-time addresses. Only this app module can read `import.meta.env`, so it hands the values over.
void Promise.all([
  loadOcharoApps(importMetaEnvSource(import.meta.env)),
  router.isReady().then(() => initializeIntegration()),
]).then(([links, ready]) => {
  // `false` means the browser is leaving for its first central sign-in.
  if (!ready) return;
  app.use(ocharoAppsPlugin(links));
  app.mount('#root');
  // The feedback host subscribes as the app mounts, so startup problems are reported once it is up.
  reportOcharoAppsStartup(links);
});
