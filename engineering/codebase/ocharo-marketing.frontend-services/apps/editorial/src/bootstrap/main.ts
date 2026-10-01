import { initializeUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { initializeIntegration } from '@ocharomarketing/connect';
import { createApp } from 'vue';
import AppRoot from './AppRoot.vue';
import { blogRouter } from './BlogRouter';
import './index.css';

initializeUiLocale();
const app = createApp(AppRoot);
app.use(blogRouter);
void blogRouter.isReady().then(async () => {
  await initializeIntegration();
  app.mount('#root');
});
