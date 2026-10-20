<script setup lang="ts">
import { watch } from 'vue';
import { RouterView, useRoute } from 'vue-router';
import { provideLocale } from '@wow-two-beta/fe/foundation/i18n';
import { ColorModeProvider } from '@wow-two-beta/fe/foundation/primitives';
import { FeedbackToastHost } from '@wow-two-beta/fe/presentation/feedback';
import { RouteAnnouncer } from '@wow-two-beta/fe/router';
import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

import { DocumentTitleSuffix } from './BlogRouter';
import { ThemeStorageKey } from './theme';

/**
 * Composes the colour mode, the SDK copy in the interface language, app feedback and the routed Ocharo frame around
 * every Studio place: stories, topics and site settings.
 */
defineOptions({ name: 'AppRoot' });
const route = useRoute();
const { t, uiLocale, sdkMessages } = useUiLocale();
provideLocale(uiLocale, sdkMessages);
// The router names the document on every navigation; a language change renames the open place.
watch(uiLocale, () => {
  const label = route.meta.label;
  document.title = typeof label === 'string' ? `${t(label)} · ${DocumentTitleSuffix}` : DocumentTitleSuffix;
});
</script>

<template>
  <ColorModeProvider :storage-key="ThemeStorageKey">
    <RouterView />
    <FeedbackToastHost :bus="feedbackBus" />
    <RouteAnnouncer />
  </ColorModeProvider>
</template>
