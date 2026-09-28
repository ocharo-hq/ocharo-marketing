import { createApp } from 'vue';
import { createMarketingWorkspace, MarketingWorkspaceKey } from '../application/marketing';
import AppRoot from './AppRoot.vue';
import { marketingRouter } from './MarketingRouter';
import './index.css';

const app = createApp(AppRoot);
const workspace = createMarketingWorkspace((path) => {
  void marketingRouter.push(path);
});
app.provide(MarketingWorkspaceKey, workspace);
app.use(marketingRouter);
void marketingRouter.isReady().then(() => {
  app.mount('#root');
});
