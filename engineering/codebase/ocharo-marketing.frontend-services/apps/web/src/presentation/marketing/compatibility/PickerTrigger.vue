<script setup lang="ts">
import { SelectPickerTrigger, type SelectPickerTriggerProps } from '@wow-two-beta/fe/presentation/forms';
/** SDK 0.0.41 still omits native accessible naming (`aria-label`) from `SelectPickerTriggerProps`; delete once declared. */
interface Props extends /* @vue-ignore */ SelectPickerTriggerProps {
  // eslint-disable-next-line vue/prop-name-casing -- Native attribute absent from the published declaration.
  'aria-label'?: string;
}
defineOptions({ name: 'PickerTrigger' });
const props = defineProps<Props>();
</script>
<template>
  <SelectPickerTrigger v-bind="props"><slot /></SelectPickerTrigger>
</template>
