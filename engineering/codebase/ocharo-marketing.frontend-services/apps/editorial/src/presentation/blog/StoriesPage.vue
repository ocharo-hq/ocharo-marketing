<script setup lang="ts">
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import { requestFailureMessage } from '@ocharomarketing/connect/RequestFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { ref as importRef } from 'vue';
import { integrationFetch } from '@ocharomarketing/connect';
const importResult = importRef('');
async function importResearch() {
  try {
    const response = await integrationFetch('/api/blog/research/import', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: '{}',
    });
    if (!response.ok) {
      store.fail(requestFailureMessage({ code: 'http', status: response.status }));
      return;
    }
    const result = await response.json();
    importResult.value = `Imported ${result.created.length} drafts; preserved ${result.skipped.length} existing items.`;
    await list.load();
  } catch {
    store.fail(requestFailureMessage({ code: 'transport', status: 0 }));
  }
}

import { onMounted } from 'vue';
import { RouterLink } from 'vue-router';
import { Eye, ImageIcon, Search, ShoppingBag } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { SkeletonState } from '@wow-two-beta/fe/presentation/feedback';
import { useArticleList, useBlogStore } from '../../application/blog';
import { languageLabels, statusLabels, publicationPath } from '../../domain/blog/BlogRules';
import StatusBadge from './components/StatusBadge.vue';
const { t: ui, formatWhen } = useUiLocale();

/** Every story with status, topic and search filters. */
defineOptions({ name: 'StoriesPage' });
const store = useBlogStore();
const list = useArticleList();
function when(value: string | null) {
  return formatWhen(value);
}
onMounted(() => list.load());
</script>

<template>
  <section class="panel mb-6">
    <h2 class="font-editorial text-2xl">{{ ui('Editorial publications') }}</h2>
    <p class="my-3 text-sm">{{ ui('Atlas, Wear and Blog share reviewed revisions. Research enters as drafts.') }}</p>
    <div class="flex flex-wrap gap-3">
      <a class="btn-ghost" href="/preview/apps/atlas/" target="_blank">{{ ui('Preview Atlas ↗') }}</a
      ><a class="btn-ghost" href="/preview/apps/wear/" target="_blank">{{ ui('Preview Wear ↗') }}</a
      ><a class="btn-ghost" href="/catalogue/" target="_blank">{{ ui('Catalogue ↗') }}</a
      ><button class="btn-primary" @click="importResearch">{{ ui('Import research drafts') }}</button>
    </div>
    <p role="status" class="mt-3 text-sm">{{ ui(importResult) }}</p>
  </section>
  <section>
    <h1 class="font-editorial text-4xl tracking-tight">{{ ui('Stories') }}</h1>
    <p class="mt-2 text-sm text-muted-foreground">
      <SkeletonState v-if="!list.hasLoaded.value && list.loading.value" class="inline-block h-4 w-8" /><span
        v-else-if="list.hasLoaded.value"
        >{{ list.articles.value.length }}</span
      >
      {{ ui('stories. Published stories appear on the site, in RSS and the sitemap.') }}
    </p>
    <div class="panel mt-6 grid gap-3 p-4 md:grid-cols-[1fr_auto_auto] xl:grid-cols-[1fr_auto_auto_auto_auto]">
      <label class="relative">
        <span class="sr-only">{{ ui('Search') }}</span>
        <Icon :icon="Search" :size="16" class="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <input v-model="list.filter.q" class="field pl-9" :placeholder="ui('Title, URL or description')" />
      </label>
      <MarketingSelect
        v-model="list.filter.publication"
        class="md:w-48"
        :label="ui('Publication')"
        :options="[
          { value: '' as const, label: ui('All publications') },
          { value: 'atlas' as const, label: 'Atlas' },
          { value: 'wear' as const, label: 'Wear' },
          { value: 'blog' as const, label: ui('Blog') },
        ]"
      />
      <MarketingSelect
        v-model="list.filter.status"
        class="md:w-48"
        :label="ui('Status')"
        :options="[
          { value: '' as const, label: ui('All statuses') },
          ...Object.entries(statusLabels).map(([value, label]) => ({ value, label: ui(label) })),
        ]"
      />
      <MarketingSelect
        v-model="list.filter.topic"
        class="md:w-48"
        :label="ui('Topic')"
        :options="[
          { value: '' as const, label: ui('All topics') },
          ...store.topics.value.map((t) => ({ value: t.key, label: t.short })),
        ]"
      />
      <MarketingSelect
        v-model="list.filter.language"
        class="md:w-40"
        :label="ui('Language')"
        :options="[
          { value: '' as const, label: ui('All languages') },
          ...Object.entries(languageLabels).map(([value, label]) => ({ value, label: ui(label) })),
        ]"
      />
    </div>
    <div class="mt-4 flex justify-end">
      <Button
        variant="outline"
        :is-disabled="list.loading.value || list.refreshing.value"
        :is-loading="list.refreshing.value"
        @click="list.refresh()"
        >{{ ui('Refresh') }}</Button
      >
    </div>
    <div class="query-region mt-4" :aria-busy="list.loading.value || list.refreshing.value">
      <div
        v-if="list.articles.value.length || (!list.hasLoaded.value && list.loading.value)"
        class="panel overflow-x-auto p-0"
      >
        <table class="w-full min-w-[820px] text-sm">
          <thead class="bg-muted/60 text-left text-[11px] uppercase tracking-[0.12em] text-muted-foreground">
            <tr>
              <th class="px-4 py-3 font-semibold">{{ ui('Story') }}</th>
              <th class="px-4 py-3 font-semibold">{{ ui('Topic') }}</th>
              <th class="px-4 py-3 font-semibold">{{ ui('Status') }}</th>
              <th class="px-4 py-3 font-semibold">{{ ui('Publication') }}</th>
              <th class="px-4 py-3 font-semibold">{{ ui('Read') }}</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            <template v-if="!list.hasLoaded.value && list.loading.value"
              ><tr v-for="row in 5" :key="row" class="border-t border-border" aria-hidden="true">
                <td v-for="cell in 6" :key="cell" class="px-4 py-3"><SkeletonState class="h-5 w-full" /></td></tr
            ></template>
            <tr v-for="a in list.articles.value" :key="a.id" class="border-t border-border hover:bg-background">
              <td class="max-w-md px-4 py-3">
                <RouterLink
                  :to="`/stories/${a.id}`"
                  class="font-editorial text-base leading-tight hover:text-primary"
                  >{{ a.title }}</RouterLink
                >
                <p class="mt-1 truncate text-xs text-muted-foreground">
                  <span class="chip mr-1 bg-muted text-muted-foreground">{{ a.language.toUpperCase() }}</span
                  >{{ publicationPath(a) }}
                </p>
              </td>
              <td class="px-4 py-3">
                <SkeletonState v-if="list.loading.value || list.refreshing.value" class="h-5 w-20" /><span v-else>{{
                  store.topicName(a.topic)
                }}</span>
              </td>
              <td class="px-4 py-3">
                <SkeletonState v-if="list.loading.value || list.refreshing.value" class="h-5 w-20" /><StatusBadge
                  v-else
                  :status="a.status"
                />
              </td>
              <td class="px-4 py-3 text-xs text-muted-foreground">
                {{ a.status === 'scheduled' ? `⏱ ${when(a.publishAt)}` : when(a.publishedAt) }}
              </td>
              <td class="px-4 py-3 text-xs text-muted-foreground">
                {{ a.readMinutes }} {{ ui('min ·') }} {{ a.words }} {{ ui('words') }}
                <Icon v-if="a.hasCover" :icon="ImageIcon" :size="12" class="ml-1 inline" />
                <Icon v-if="a.productCount" :icon="ShoppingBag" :size="12" class="ml-1 inline" />
              </td>
              <td class="px-4 py-3 text-right">
                <a
                  :href="store.links.preview(a.id)"
                  target="_blank"
                  rel="noopener"
                  class="btn-ghost px-2"
                  :aria-label="ui('View {title}', { title: a.title })"
                  ><Icon :icon="Eye" :size="15"
                /></a>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="list.hasLoaded.value && !list.articles.value.length" class="query-label">{{ ui('Story not found.') }}</p>
      <p v-else-if="!list.hasLoaded.value && !list.loading.value" class="query-label" role="status">
        {{ ui('Content unavailable. Try refreshing.') }}
      </p>
    </div>
  </section>
</template>
