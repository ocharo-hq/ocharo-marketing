import { createAppRouter } from '@wow-two-beta/fe/router';
/** Composes code-split, directly addressable marketing pages. */
export const marketingRouter = createAppRouter(
  [
    { path: '/', lazy: () => import('../presentation/marketing/TodayPage.vue'), handle: { title: 'Bugun' } },
    {
      path: '/channels',
      lazy: () => import('../presentation/marketing/ChannelsPage.vue'),
      handle: { title: 'Kanallar xaritasi' },
    },
    {
      path: '/monitoring',
      lazy: () => import('../presentation/marketing/MonitoringPage.vue'),
      handle: { title: 'Monitoring' },
    },
    { path: '/posts', lazy: () => import('../presentation/marketing/PostsPage.vue'), handle: { title: 'Postlar' } },
    { path: '/posts/new', lazy: () => import('./PostComposerRoute.vue'), handle: { title: 'Yangi post' } },
    { path: '/posts/:id', lazy: () => import('./PostComposerRoute.vue'), handle: { title: 'Postni tayyorlash' } },
    {
      path: '/products',
      lazy: () => import('../presentation/marketing/ProductsPage.vue'),
      handle: { title: 'Mahsulotlar' },
    },
    {
      path: '/creative-imports',
      lazy: () => import('../presentation/marketing/CreativeImportsPage.vue'),
      handle: { title: 'Studio imports' },
    },
    {
      path: '/inquiries',
      lazy: () => import('../presentation/marketing/InquiriesPage.vue'),
      handle: { title: 'Enquiries' },
    },
    { path: '/atlas', lazy: () => import('../presentation/marketing/AtlasPage.vue'), handle: { title: 'Atlas' } },
    {
      path: '/calendar',
      lazy: () => import('../presentation/marketing/CalendarPage.vue'),
      handle: { title: 'Taqvim' },
    },
    {
      path: '/results',
      lazy: () => import('../presentation/marketing/ResultsPage.vue'),
      handle: { title: 'Natijalar' },
    },
    {
      path: '/automation',
      lazy: () => import('../presentation/marketing/AutomationPage.vue'),
      handle: { title: 'Avtomatika' },
    },
    {
      path: '/connections',
      lazy: () => import('../presentation/marketing/ConnectionsPage.vue'),
      handle: { title: 'Ulanishlar' },
    },
    {
      path: '/:pathMatch(.*)*',
      lazy: () => import('../presentation/marketing/NotFoundPage.vue'),
      handle: { title: 'Sahifa topilmadi' },
    },
  ],
  { titleSuffix: 'Ocharo Marketing', routePersistence: false },
);
