<script setup lang="ts">
import { SelectPickerTrigger, type SelectPickerTriggerProps } from '@wow-two-beta/ui-vue/presentation/forms';
/** Defines the SDK trigger with its runtime-supported accessible label. */
interface Props extends /* @vue-ignore */ SelectPickerTriggerProps {
  /** Names the native trigger for assistive technology. */
  // eslint-disable-next-line vue/prop-name-casing -- Declares the native ARIA attribute missing from SDK types.
  'aria-label'?: string;
}
/** Preserves the SDK compound trigger and forwards a typed accessible name. */
defineOptions({ name: 'PickerTrigger' });
const props = defineProps<Props>();
</script>
<template>
  <SelectPickerTrigger v-bind="props"><slot /></SelectPickerTrigger>
</template>
