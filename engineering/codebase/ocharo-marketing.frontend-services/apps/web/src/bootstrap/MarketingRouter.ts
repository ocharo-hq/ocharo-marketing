import { createAppRouter, type RouteConfig, type RouteHandle } from '@wow-two-beta/fe/router';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import type { MarketingSection } from '../domain/shared/MarketingSections';
import AppLayout from './AppLayout.vue';

/** The app name after each place's title in the document title. */
export const DocumentTitleSuffix = 'Ocharo Marketing';

const { t } = useUiLocale();

/**
 * A place's metadata. `label` is the English source the breadcrumb and the document title translate; `section` names
 * the sidebar entry the place belongs to, `null` for a place outside every section.
 */
function place(label: string, section: MarketingSection | null): RouteHandle {
  return { label, section, title: () => t(label) };
}

/** Code-split, directly addressable marketing places, every one inside the Ocharo frame. */
export const marketingRoutes: RouteConfig = [
  {
    layout: AppLayout,
    children: [
      {
        index: true,
        lazy: () => import('../presentation/common/pages/TodayPage.vue'),
        handle: place('Today', 'today'),
      },
      {
        path: 'posts',
        lazy: () => import('../presentation/posts/pages/PostsPage.vue'),
        handle: place('Posts', 'posts'),
      },
      { path: 'posts/new', lazy: () => import('./PostComposerRoute.vue'), handle: place('New post', 'posts') },
      { path: 'posts/:id', lazy: () => import('./PostComposerRoute.vue'), handle: place('Prepare post', 'posts') },
      {
        path: 'products',
        lazy: () => import('../presentation/products/ProductsPage.vue'),
        handle: place('Products', 'products'),
      },
      {
        path: 'atlas',
        lazy: () => import('../presentation/posts/pages/AtlasPage.vue'),
        handle: place('Atlas', 'atlas'),
      },
      {
        path: 'creative-imports',
        lazy: () => import('../presentation/posts/pages/CreativeImportsPage.vue'),
        handle: place('Studio imports', 'creative-imports'),
      },
      {
        path: 'calendar',
        lazy: () => import('../presentation/delivery/pages/CalendarPage.vue'),
        handle: place('Calendar', 'calendar'),
      },
      {
        path: 'automation',
        lazy: () => import('../presentation/automation/AutomationPage.vue'),
        handle: place('Automation', 'automation'),
      },
      {
        path: 'channels',
        lazy: () => import('../presentation/channels/ChannelsPage.vue'),
        handle: place('Channels', 'channels'),
      },
      {
        path: 'inquiries',
        lazy: () => import('../presentation/inquiries/InquiriesPage.vue'),
        handle: place('Enquiries', 'inquiries'),
      },
      {
        path: 'monitoring',
        lazy: () => import('../presentation/delivery/pages/MonitoringPage.vue'),
        handle: place('Monitoring', 'monitoring'),
      },
      {
        path: 'results',
        lazy: () => import('../presentation/results/ResultsPage.vue'),
        handle: place('Results', 'results'),
      },
      {
        path: 'connections',
        lazy: () => import('../presentation/connections/ConnectionsPage.vue'),
        handle: place('Connections', 'connections'),
      },
      {
        path: ':pathMatch(.*)*',
        lazy: () => import('../presentation/common/pages/NotFoundPage.vue'),
        handle: place('Page not found', null),
      },
    ],
  },
];

/** Creates the router over `marketingRoutes`; tests pass memory history. */
export function createMarketingRouter(options: Parameters<typeof createAppRouter>[1] = {}) {
  return createAppRouter(marketingRoutes, { titleSuffix: DocumentTitleSuffix, routePersistence: false, ...options });
}
