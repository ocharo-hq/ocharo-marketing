<script setup lang="ts">
import { FilePicker, type FilePickerProps } from '@wow-two-beta/fe/presentation/forms';
/** SDK 0.0.31 still omits the native accept attribute from its component declaration. */
interface Props extends /* @vue-ignore */ FilePickerProps {
  accept?: string;
}
defineOptions({ name: 'MediaPicker' });
const props = defineProps<Props>();
const emit = defineEmits<{ 'files-change': [files: FileList | null] }>();
</script>
<template><FilePicker v-bind="props" @files-change="emit('files-change', $event)" /></template>
