<script setup lang="ts">
import { computed } from 'vue';
import { SkeletonState } from '@wow-two-beta/ui-vue/presentation/feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
const props = defineProps<{ path: string }>();
const { t: ui } = useUiLocale();
const form = computed(() => /^\/posts\//.test(props.path));
const gallery = computed(() => ['/posts', '/products'].includes(props.path));
</script>
<template>
  <section class="query-region space-y-6" aria-busy="true">
    <span role="status" class="sr-only">{{ ui('Loading workspace…') }}</span>
    <SkeletonState class="h-9 w-56" />
    <SkeletonState class="h-5 w-2/3" />
    <div v-if="form" class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]" aria-hidden="true">
      <div class="space-y-6">
        <SkeletonState class="h-11 w-full" /><SkeletonState class="h-52 w-full" /><SkeletonState class="h-11 w-full" />
      </div>
      <SkeletonState class="aspect-[4/5] w-full" />
    </div>
    <div
      v-else-if="path === '/creative-imports'"
      class="space-y-4 rounded-2xl border border-border bg-card p-6"
      aria-hidden="true"
    >
      <SkeletonState class="h-5 w-48" /><SkeletonState class="h-28 w-full" />
      <div class="grid gap-4 sm:grid-cols-2">
        <SkeletonState class="h-11 w-full" /><SkeletonState class="h-11 w-full" />
      </div>
      <SkeletonState class="h-11 w-32" />
    </div>
    <div
      v-else-if="path === '/calendar'"
      class="overflow-hidden rounded-2xl border border-border bg-card"
      aria-hidden="true"
    >
      <div class="border-b border-border px-6 py-5"><SkeletonState class="h-5 w-56" /></div>
      <div class="grid grid-cols-1 divide-y divide-border md:grid-cols-7 md:divide-x md:divide-y-0">
        <div v-for="day in 7" :key="day" class="min-h-48 p-3"><SkeletonState class="h-7 w-full" /></div>
      </div>
    </div>
    <div v-else-if="path === '/inquiries'" class="space-y-5" aria-hidden="true">
      <div class="flex flex-wrap gap-3 rounded-2xl border border-border bg-card p-4">
        <SkeletonState class="h-11 min-w-48 flex-1" /><SkeletonState class="h-11 w-32" /><SkeletonState
          class="h-11 w-32"
        />
      </div>
      <div v-for="card in 3" :key="card" class="space-y-4 rounded-2xl border border-border bg-card p-5 sm:p-6">
        <SkeletonState class="h-8 w-1/3" /><SkeletonState class="h-5 w-2/3" /><SkeletonState class="h-16 w-full" />
        <div class="grid gap-3 sm:grid-cols-2">
          <SkeletonState class="h-11 w-full" /><SkeletonState class="h-11 w-full" />
        </div>
        <SkeletonState class="h-24 w-full" />
      </div>
    </div>
    <div v-else-if="gallery" class="space-y-5" aria-hidden="true">
      <SkeletonState class="h-11 w-full max-w-sm" />
      <div class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
        <div v-for="card in 3" :key="card" class="overflow-hidden rounded-2xl border border-border bg-card">
          <SkeletonState class="aspect-[4/3] w-full" />
          <div class="space-y-4 p-5">
            <SkeletonState class="h-6 w-2/3" /><SkeletonState class="h-5 w-full" /><SkeletonState class="h-5 w-1/2" />
          </div>
        </div>
      </div>
    </div>
    <div v-else class="grid gap-5 md:grid-cols-2 xl:grid-cols-3" aria-hidden="true">
      <div v-for="card in 3" :key="card" class="rounded-2xl border border-border bg-card p-6 space-y-4">
        <SkeletonState class="h-5 w-2/3" /><SkeletonState class="h-10 w-1/2" />
      </div>
    </div>
  </section>
</template>
