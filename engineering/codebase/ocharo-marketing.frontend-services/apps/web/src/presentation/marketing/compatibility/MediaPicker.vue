<script setup lang="ts">
import { FilePicker, type FilePickerProps } from '@wow-two-beta/ui-vue/presentation/forms';
/** Defines the SDK picker with its runtime-supported file constraint. */
interface Props extends /* @vue-ignore */ FilePickerProps {
  /** Lists the supported source MIME types for the native file chooser. */
  accept?: string;
}
/** Keeps native file selection inside the published SDK picker. */
defineOptions({ name: 'MediaPicker' });
const props = defineProps<Props>();
const emit = defineEmits<{ 'files-change': [files: FileList | null] }>();
</script>
<template><FilePicker v-bind="props" @files-change="emit('files-change', $event)" /></template>
