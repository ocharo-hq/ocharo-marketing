<script setup lang="ts">
import { SelectPickerTrigger, type SelectPickerTriggerProps } from '@wow-two-beta/fe/presentation/forms';
/** SDK 0.0.28 still omits native accessible naming from this component declaration. */
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
