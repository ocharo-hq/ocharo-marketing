<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { ImageEditor, type ImageEditRecipe } from '@wow-two-beta/ui-vue/presentation/forms';
import { ImageEditRecipeExtensions } from '@wow-two-beta/ui-vue/domain/imageEditing';
import { useMediaEditing } from '../../../application/blog/UseMediaEditing';
import type { MediaView } from '../../../domain/blog/BlogModels';

defineOptions({ name: 'CoverImageEditor' });
const props = defineProps<{ media: MediaView; alt: string; canEdit: boolean; disabled?: boolean }>();
const emit = defineEmits<{
  applied: [media: MediaView];
  close: [];
  busy: [value: boolean];
  pendingEdits: [value: boolean];
}>();
const natural = ref<{ width: number; height: number } | null>(null),
  sourceError = ref('');
const originalUrl = computed(() => (/^[a-f0-9]{64}$/.test(props.media.sha256) ? `/media/${props.media.sha256}` : ''));
const source = computed(() => ({
  ...props.media,
  url: originalUrl.value,
  width: natural.value?.width ?? null,
  height: natural.value?.height ?? null,
}));
watch(
  () => props.media.sha256,
  () => {
    natural.value = null;
    sourceError.value = '';
  },
  { flush: 'sync' },
);
function loaded(event: Event): void {
  const image = event.target as HTMLImageElement;
  if (image.getAttribute('src') !== originalUrl.value) return;
  if (!image.naturalWidth || !image.naturalHeight) {
    sourceError.value = 'This image has no readable dimensions.';
    return;
  }
  natural.value = { width: image.naturalWidth, height: image.naturalHeight };
  sourceError.value = '';
}
function failed(event: Event): void {
  if ((event.target as HTMLImageElement).getAttribute('src') === originalUrl.value)
    sourceError.value = 'The original image could not be loaded. Close the tools and retry; your cover is unchanged.';
}
const editing = useMediaEditing(
  () => source.value,
  () => props.canEdit && !props.disabled,
);
watch(
  () => editing.busy.value || editing.loading.value,
  (value) => emit('busy', value),
  { immediate: true, flush: 'sync' },
);
const initialRecipe = JSON.stringify(ImageEditRecipeExtensions.create());
watch(
  () => editing.busy.value || JSON.stringify(editing.recipe.value) !== initialRecipe,
  (value) => emit('pendingEdits', value),
  { immediate: true, flush: 'sync' },
);
onScopeDispose(() => {
  emit('busy', false);
  emit('pendingEdits', false);
});
async function apply(recipe: ImageEditRecipe): Promise<void> {
  const result = await editing.apply(recipe);
  if (result) emit('applied', result);
}
</script>

<template>
  <section class="image-workbench my-4 rounded-xl border border-border bg-card p-4" aria-label="Cover image editing">
    <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
      <h3 class="font-semibold">Edit cover image</h3>
      <button type="button" class="btn-ghost" @click="emit('close')">Cancel image edits</button>
    </div>
    <p class="mb-3 text-sm text-muted-foreground">
      Apply creates a new image. Captions and credits stay with this draft. Save the story when ready.
    </p>
    <p v-if="editing.loading.value" role="status">Loading image tools…</p>
    <img
      v-if="originalUrl"
      :key="originalUrl"
      :src="originalUrl"
      alt=""
      class="hidden"
      aria-hidden="true"
      @load="loaded"
      @error="failed"
    />
    <p v-if="sourceError || !originalUrl" role="alert">
      {{ sourceError || 'This image is unavailable. Choose a saved image from the media library.' }}
    </p>
    <p v-else-if="!natural" role="status">Loading original image dimensions…</p>
    <template v-if="editing.capabilities.value && editing.sourceReady.value">
      <ImageEditor
        :src="source.url"
        :alt="alt"
        :natural-width="source.width!"
        :natural-height="source.height!"
        :model-value="editing.recipe.value"
        :preview-src="editing.previewUrl.value || undefined"
        :is-busy="editing.busy.value"
        :error="editing.error.value || undefined"
        :is-disabled="!canEdit || disabled"
        :can-remove-background="editing.capabilities.value.backgroundRemoval.available"
        :max-dimension="editing.capabilities.value.maximumDimension"
        :max-pixels="editing.capabilities.value.maximumPixels"
        @update:model-value="editing.update"
        @preview="editing.preview"
        @apply="apply"
      />
      <p v-if="editing.recipe.value.output.format === 'jpeg'" class="mt-2 text-xs text-muted-foreground">
        JPEG fills transparent areas with white. Choose PNG or WebP to keep transparency.
      </p>
      <p v-if="!editing.capabilities.value.backgroundRemoval.available" class="mt-2 text-xs text-muted-foreground">
        Automatic background removal is unavailable on this host. Crop, rotation and resizing are available.
      </p>
    </template>
    <template v-else-if="editing.error.value"
      ><p role="alert" class="text-sm text-destructive">{{ editing.error.value }}</p>
      <button type="button" class="btn-outline mt-2" :disabled="editing.loading.value" @click="editing.load">
        Retry image tools
      </button></template
    >
  </section>
</template>

<style scoped>
.image-workbench {
  min-width: 0;
}
.image-workbench :deep(img) {
  max-width: 100%;
}
</style>
