<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, ref } from 'vue';
import { ArrowUpRight, Plus, Search } from 'lucide-vue-next';

import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { TextInput } from '@wow-two-beta/fe/presentation/forms';

import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions } from '../../domain/marketing';
import StatusIndicator from './StatusIndicator.vue';
const { t: ui, formatDate } = useUiLocale();

/** Lists saved post revisions with their actual review status. */
defineOptions({ name: 'PostsPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const search = ref('');
const filter = ref('all');
/** The list follows the current search and review filter. */
const posts = computed(() =>
  [...(workspace.value?.drafts ?? [])]
    .filter(
      (post) =>
        (filter.value === 'all' || post.status === filter.value) &&
        post.title.toLocaleLowerCase().includes(search.value.toLocaleLowerCase()),
    )
    .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)),
);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between gap-4">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Content library') }}</p>
        <h1 class="mt-2 font-editorial text-3xl">{{ ui('Posts') }}</h1>
        <p class="mt-3 text-sm text-muted-foreground">
          {{ ui('Every caption, selected image and reviewed variant in one place.') }}
        </p>
      </div>
      <Button @click="desk.navigate('/posts/new')"
        ><Icon :icon="Plus" :size="17" class="mr-2" />{{ ui('New post') }}</Button
      >
    </div>
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div class="flex gap-1 rounded-lg bg-muted p-1">
        <button
          v-for="item in [
            { id: 'all', label: 'Barchasi' },
            { id: 'draft', label: 'Qoralamalar' },
            { id: 'approved', label: 'Tekshirilgan' },
          ]"
          :key="item.id"
          :class="[
            'rounded-md px-4 py-2 text-xs font-semibold',
            filter === item.id ? 'bg-card shadow-sm' : 'text-muted-foreground',
          ]"
          @click="filter = item.id"
        >
          {{ ui(item.label) }}
        </button>
      </div>
      <div class="flex w-full max-w-64 items-center gap-2">
        <Icon :icon="Search" :size="17" class="text-muted-foreground" /><TextInput
          v-model="search"
          :placeholder="ui('Find a post…')"
          :aria-label="ui('Find a post')"
        />
      </div>
    </div>
    <p v-if="!posts.length" class="query-label">
      {{ search ? ui('No matching post found') : ui('Choose an existing product image and save your first post.') }}
    </p>
    <div v-else class="query-region grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
      <button
        v-for="post in posts"
        :key="post.id"
        class="overflow-hidden rounded-2xl border border-border bg-card text-left transition hover:border-primary hover:shadow-paper"
        @click="desk.navigate(`/posts/${post.id}`)"
      >
        <div class="relative flex aspect-[4/3] items-center justify-center bg-muted p-5">
          <img
            :src="
              workspace?.assets.find((asset) => asset.id === post.mediaIds[0])?.url ??
              MarketingExtensions.productAsset(
                workspace,
                workspace?.products.find((item) => item.id === post.productId),
              ) ??
              ''
            "
            :alt="post.title"
            class="h-full w-full object-contain"
          /><span class="absolute right-3 top-3 rounded-full bg-card p-2"
            ><Icon :icon="ArrowUpRight" :size="16"
          /></span>
        </div>
        <div class="p-5">
          <div class="mb-3 flex items-center justify-between">
            <StatusIndicator :status="post.status" /><span class="text-xs text-muted-foreground">{{
              post.locale.toUpperCase()
            }}</span>
          </div>
          <h2 class="text-base font-semibold">{{ post.title }}</h2>
          <p class="mt-2 line-clamp-2 text-xs leading-6 text-muted-foreground">
            {{ post.caption || ui('No text entered') }}
          </p>
          <p class="mt-4 text-xs text-muted-foreground">
            {{ formatDate(post.updatedAt) }} · {{ post.revision }}{{ ui(' variant') }}
          </p>
        </div>
      </button>
    </div>
  </div>
</template>
