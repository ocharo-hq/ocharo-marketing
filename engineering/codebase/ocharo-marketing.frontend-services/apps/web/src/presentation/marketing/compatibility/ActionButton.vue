<script setup lang="ts">
import { Button, type ButtonProps } from '@wow-two-beta/ui-vue/presentation/actions';
/** Defines the SDK button with its runtime-supported native activation handler. */
interface Props extends /* @vue-ignore */ ButtonProps {
  /** Runs only for an accepted native click, including keyboard activation. */
  onClick?: (event: MouseEvent) => void;
}
/** Bridges the published declaration gap without replacing SDK button behavior. */
defineOptions({ name: 'ActionButton' });
const props = defineProps<Props>();
</script>
<template>
  <Button v-bind="props"><slot /></Button>
</template>
