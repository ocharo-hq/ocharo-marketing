<script setup lang="ts">
import { computed } from 'vue';

import { Badge } from '@wow-two-beta/ui-vue/presentation/display';

import { MarketingExtensions } from '../../domain/marketing';

/** Displays a recorded workflow state without deriving publication success. */
defineOptions({ name: 'StatusIndicator' });
const props = defineProps<{ status: string }>();
/** The semantic treatment follows the recorded outcome. */
const variant = computed(() =>
  ['approved', 'published', 'completed', 'confirmed'].includes(props.status)
    ? 'success'
    : ['failed', 'unknown'].includes(props.status)
      ? 'danger'
      : ['planned', 'queued', 'running', 'pending', 'TBD'].includes(props.status)
        ? 'warning'
        : 'neutral',
);
</script>

<template>
  <Badge :variant="variant" size="sm">{{ MarketingExtensions.statusLabel(status) }}</Badge>
</template>
