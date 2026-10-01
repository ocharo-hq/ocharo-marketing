<script setup lang="ts">
import { computed, ref } from 'vue';
import { ArrowUpRight, Plus, Search } from 'lucide-vue-next';

import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { TextInput } from '@wow-two-beta/ui-vue/presentation/forms';

import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions } from '../../domain/marketing';
import StatusIndicator from './StatusIndicator.vue';

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
        <p class="text-xs uppercase tracking-widest text-primary">Kontent kutubxonasi</p>
        <h1 class="mt-2 font-editorial text-3xl">Postlar</h1>
        <p class="mt-3 text-sm text-muted-foreground">
          Har bir matn, tanlangan tasvir va tekshirilgan variant bir joyda.
        </p>
      </div>
      <Button @click="desk.navigate('/posts/new')"><Icon :icon="Plus" :size="17" class="mr-2" />Yangi post</Button>
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
          {{ item.label }}
        </button>
      </div>
      <div class="flex w-full max-w-64 items-center gap-2">
        <Icon :icon="Search" :size="17" class="text-muted-foreground" /><TextInput
          v-model="search"
          placeholder="Postni topish…"
          aria-label="Postni topish"
        />
      </div>
    </div>
    <section
      v-if="!posts.length"
      class="flex min-h-80 flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card p-8 text-center"
    >
      <h2 class="font-editorial text-2xl">{{ search ? 'Mos post topilmadi' : 'Bir mahsulot. Bir yangi post.' }}</h2>
      <p class="mt-3 max-w-sm text-sm leading-7 text-muted-foreground">
        {{
          search
            ? 'Boshqa nomni sinab ko‘ring yoki filtrni o‘zgartiring.'
            : 'Mavjud mahsulot rasmini tanlang va birinchi postingizni saqlang.'
        }}
      </p>
      <Button v-if="!search" class="mt-6" @click="desk.navigate('/posts/new')">Post yaratish</Button>
    </section>
    <div v-else class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
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
            {{ post.caption || 'Matn kiritilmagan' }}
          </p>
          <p class="mt-4 text-xs text-muted-foreground">
            {{ MarketingExtensions.formatDate(post.updatedAt) }} · {{ post.revision }}-variant
          </p>
        </div>
      </button>
    </div>
  </div>
</template>
