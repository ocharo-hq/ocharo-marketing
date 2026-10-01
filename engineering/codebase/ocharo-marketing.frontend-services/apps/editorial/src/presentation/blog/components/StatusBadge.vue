<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed } from 'vue';
import type { ArticleStatus } from '../../../domain/blog/BlogModels';
import { statusLabels } from '../../../domain/blog/BlogRules';
const { t: ui } = useUiLocale();

/** Story lifecycle chip. */
defineOptions({ name: 'StatusBadge' });
const props = defineProps<{ status: ArticleStatus }>();
const tone = computed(
  () =>
    ({
      draft: 'bg-warning-soft text-warning-soft-foreground',
      scheduled: 'bg-primary-soft text-primary-soft-foreground',
      published: 'bg-success-soft text-success-soft-foreground',
      archived: 'bg-muted text-muted-foreground',
    })[props.status],
);
</script>

<template>
  <span :class="['chip', tone]">{{ ui(statusLabels[props.status]) }}</span>
</template>
