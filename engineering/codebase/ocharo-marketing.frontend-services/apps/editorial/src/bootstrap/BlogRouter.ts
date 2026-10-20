import { createAppRouter, type RouteConfig, type RouteHandle } from '@wow-two-beta/fe/router';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import type { EditorialSection } from '../domain/shared/EditorialSections';
import AppLayout from './AppLayout.vue';

/** The app name after each place's title in the document title. */
export const DocumentTitleSuffix = 'Ocharo Editorial';

const { t } = useUiLocale();

/**
 * A place's metadata. `label` is the English source the breadcrumb and the document title translate; `section` names
 * the sidebar entry the place belongs to, `null` for a place outside every section.
 */
function place(label: string, section: EditorialSection | null): RouteHandle {
  return { label, section, title: () => t(label) };
}

/** Code-split Studio places, served under /studio/ next to the public site, every one inside the Ocharo frame. */
export const blogRoutes: RouteConfig = [
  {
    layout: AppLayout,
    children: [
      { index: true, lazy: () => import('../presentation/blog/StoriesPage.vue'), handle: place('Stories', 'stories') },
      {
        path: 'stories/new',
        lazy: () => import('../presentation/blog/StoryEditorPage.vue'),
        handle: place('New story', 'stories'),
      },
      {
        path: 'stories/:id',
        lazy: () => import('../presentation/blog/StoryEditorPage.vue'),
        handle: place('Story', 'stories'),
      },
      { path: 'topics', lazy: () => import('../presentation/blog/TopicsPage.vue'), handle: place('Topics', 'topics') },
      {
        path: 'settings',
        lazy: () => import('../presentation/blog/SettingsPage.vue'),
        handle: place('Site settings', 'settings'),
      },
      {
        path: ':pathMatch(.*)*',
        lazy: () => import('../presentation/blog/NotFoundPage.vue'),
        handle: place('Page not found', null),
      },
    ],
  },
];

/** Creates the router over `blogRoutes` under the app's base; tests pass memory history. */
export function createBlogRouter(options: Parameters<typeof createAppRouter>[1] = {}) {
  return createAppRouter(blogRoutes, {
    titleSuffix: DocumentTitleSuffix,
    routePersistence: false,
    basename: import.meta.env.BASE_URL,
    ...options,
  });
}
