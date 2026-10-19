import { initializeUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { initializeIntegration, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { createApp } from 'vue';
import { sessionExpiryNotice } from '../application/shared/UseFeedback';
import AppRoot from './AppRoot.vue';
import { blogRouter } from './BlogRouter';
import './index.css';

initializeUiLocale();
// An expired sign-in shows one neutral notice; the page and its unsaved edits stay while the operator reconnects.
subscribeIntegrationExpired(sessionExpiryNotice());
const app = createApp(AppRoot);
app.use(blogRouter);
void blogRouter.isReady().then(async () => {
  if (await initializeIntegration()) app.mount('#root');
});
