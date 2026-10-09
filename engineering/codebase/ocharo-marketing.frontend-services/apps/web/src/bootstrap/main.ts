import { initializeUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { initializeIntegration } from '@ocharomarketing/connect';
import { createApp } from 'vue';
import { createMarketingWorkspace, MarketingWorkspaceKey } from '../application/marketing';
import AppRoot from './AppRoot.vue';
import { marketingRouter } from './MarketingRouter';
import './index.css';

initializeUiLocale();
const app = createApp(AppRoot);
const workspace = createMarketingWorkspace((path) => {
  void marketingRouter.push(path);
});
app.provide(MarketingWorkspaceKey, workspace);
app.use(marketingRouter);
void marketingRouter.isReady().then(async () => {
  if (await initializeIntegration()) app.mount('#root');
});
