import { initializeUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { initializeIntegration, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { createApp } from 'vue';
import { createMarketingWorkspace, MarketingWorkspaceKey } from '../application/marketing';
import { sessionExpiryNotice } from '../application/shared/UseFeedback';
import AppRoot from './AppRoot.vue';
import { marketingRouter } from './MarketingRouter';
import './index.css';

initializeUiLocale();
// An expired sign-in shows one neutral notice; the page and its unsaved edits stay while the operator reconnects.
subscribeIntegrationExpired(sessionExpiryNotice());
const app = createApp(AppRoot);
const workspace = createMarketingWorkspace((path) => {
  void marketingRouter.push(path);
});
app.provide(MarketingWorkspaceKey, workspace);
app.use(marketingRouter);
void marketingRouter.isReady().then(async () => {
  if (await initializeIntegration()) app.mount('#root');
});
