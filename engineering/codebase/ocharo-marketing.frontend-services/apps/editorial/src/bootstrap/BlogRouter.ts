import { createAppRouter } from '@wow-two-beta/ui-vue/router';

/** Code-split Studio pages, served under /studio/ next to the public site. */
export const blogRouter = createAppRouter(
  [
    { path: '/', lazy: () => import('../presentation/blog/StoriesPage.vue'), handle: { title: 'Hikoyalar' } },
    {
      path: '/stories/new',
      lazy: () => import('../presentation/blog/StoryEditorPage.vue'),
      handle: { title: 'Yangi hikoya' },
    },
    {
      path: '/stories/:id',
      lazy: () => import('../presentation/blog/StoryEditorPage.vue'),
      handle: { title: 'Hikoya' },
    },
    { path: '/topics', lazy: () => import('../presentation/blog/TopicsPage.vue'), handle: { title: 'Bo‘limlar' } },
    { path: '/settings', lazy: () => import('../presentation/blog/SettingsPage.vue'), handle: { title: 'Sayt' } },
    {
      path: '/:pathMatch(.*)*',
      lazy: () => import('../presentation/blog/NotFoundPage.vue'),
      handle: { title: 'Topilmadi' },
    },
  ],
  { titleSuffix: 'Ocharo Blog Studio', routePersistence: false, basename: import.meta.env.BASE_URL },
);
