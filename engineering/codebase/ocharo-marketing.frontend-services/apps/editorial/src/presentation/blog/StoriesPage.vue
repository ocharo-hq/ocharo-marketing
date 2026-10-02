<script setup lang="ts">
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
    if (!response.ok) { store.fail(requestFailureMessage({ code: 'http', status: response.status })); return; }
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
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
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
      {{ list.articles.value.length }}
      {{ ui('stories. Published stories appear on the site, in RSS and the sitemap.') }}
    </p>
    <div class="panel mt-6 grid gap-3 p-4 md:grid-cols-[1fr_auto_auto] xl:grid-cols-[1fr_auto_auto_auto_auto]">
      <label class="relative">
        <span class="sr-only">{{ ui('Search') }}</span>
        <Icon :icon="Search" :size="16" class="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <input v-model="list.filter.q" class="field pl-9" :placeholder="ui('Title, URL or description')" />
      </label>
      <select v-model="list.filter.publication" class="field md:w-48" :aria-label="ui('Publication')">
        <option value="">{{ ui('All publications') }}</option>
        <option value="atlas">Atlas</option>
        <option value="wear">Wear</option>
        <option value="blog">{{ ui('Blog') }}</option>
      </select>
      <select v-model="list.filter.status" class="field md:w-48" :aria-label="ui('Status')">
        <option value="">{{ ui('All statuses') }}</option>
        <option v-for="(label, key) in statusLabels" :key="key" :value="key">{{ ui(label) }}</option>
      </select>
      <select v-model="list.filter.topic" class="field md:w-48" :aria-label="ui('Topic')">
        <option value="">{{ ui('All topics') }}</option>
        <option v-for="t in store.topics.value" :key="t.key" :value="t.key">{{ t.short }}</option>
      </select>
      <select v-model="list.filter.language" class="field md:w-40" :aria-label="ui('Language')">
        <option value="">{{ ui('All languages') }}</option>
        <option v-for="(label, key) in languageLabels" :key="key" :value="key">{{ ui(label) }}</option>
      </select>
    </div>
    <div v-if="list.loading.value" aria-busy="true" :aria-label="ui('Loading workspace…')" class="panel mt-4 animate-pulse"><div v-for="row in 5" :key="row" class="my-4 h-10 rounded bg-muted" /></div>
    <div v-show="!list.loading.value" class="panel mt-4 overflow-x-auto p-0">
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
          <tr v-for="a in list.articles.value" :key="a.id" class="border-t border-border hover:bg-background">
            <td class="max-w-md px-4 py-3">
              <RouterLink :to="`/stories/${a.id}`" class="font-editorial text-base leading-tight hover:text-primary">{{
                a.title
              }}</RouterLink>
              <p class="mt-1 truncate text-xs text-muted-foreground">
                <span class="chip mr-1 bg-secondary text-secondary-foreground">{{ a.language.toUpperCase() }}</span
                >{{ publicationPath(a) }}
              </p>
            </td>
            <td class="px-4 py-3">{{ store.topicName(a.topic) }}</td>
            <td class="px-4 py-3"><StatusBadge :status="a.status" /></td>
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
      <p
        v-if="!list.articles.value.length && !list.loading.value"
        class="p-10 text-center text-sm text-muted-foreground"
      >
        {{ ui('Story not found.') }}
      </p>
    </div>
  </section>
</template>
