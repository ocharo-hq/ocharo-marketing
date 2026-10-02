<script setup lang="ts">
import { SkeletonState } from '@wow-two-beta/ui-vue/presentation/feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
withDefaults(defineProps<{ kind?: 'story' | 'settings' }>(), { kind: 'story' });
const { t: ui } = useUiLocale();
</script>
<template>
  <section class="query-region space-y-6" aria-busy="true">
    <span class="sr-only" role="status">{{ ui('Loading workspace…') }}</span>
    <div
      :class="kind === 'story' ? 'grid gap-6 2xl:grid-cols-[minmax(0,1fr)_minmax(460px,0.9fr)]' : 'space-y-6'"
      aria-hidden="true"
    >
      <div class="space-y-6">
        <section v-for="section in kind === 'settings' ? 3 : 2" :key="section" class="panel space-y-4">
          <SkeletonState class="h-8 w-48" />
          <div class="grid gap-4 md:grid-cols-2">
            <div v-for="field in section === 1 ? 2 : 6" :key="field" class="space-y-2">
              <SkeletonState class="h-4 w-28" /><SkeletonState class="h-11 w-full" />
            </div>
          </div>
          <SkeletonState v-if="kind === 'story'" class="h-40 w-full" />
        </section>
      </div>
      <div v-if="kind === 'story'" class="space-y-4">
        <SkeletonState class="h-8 w-36" /><SkeletonState class="h-[650px] w-full" />
      </div>
    </div>
  </section>
</template>
