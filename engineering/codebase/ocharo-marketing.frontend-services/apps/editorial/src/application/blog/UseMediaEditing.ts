import { computed, onScopeDispose, ref, shallowRef, watch } from 'vue';
import { ImageEditRecipeExtensions, type ImageEditRecipe } from '@wow-two-beta/fe/domain/imageEditing';
import type { MediaView } from '../../domain/blog/BlogModels';
import type { MediaEditingCapabilities } from '../../domain/blog/MediaEditingModels';
import { blogApi, describeFailure } from '../../integration/blog';

/** Owns cancellable preview processing; explicit application creates a separate immutable asset. */
export function useMediaEditing(source: () => MediaView, allowed: () => boolean) {
  const recipe = ref<ImageEditRecipe>(ImageEditRecipeExtensions.create());
  const capabilities = shallowRef<MediaEditingCapabilities | null>(null);
  const previewUrl = ref(''),
    error = ref(''),
    busy = ref(false),
    loading = ref(false);
  const sourceReady = computed(() => !!source().width && !!source().height);
  const locked = computed(() => !allowed() || busy.value || loading.value || !capabilities.value || !sourceReady.value);
  let controller: AbortController | undefined,
    sequence = 0,
    disposed = false;
  function clearPreview(): void {
    if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
    previewUrl.value = '';
  }
  function cancel(): void {
    sequence++;
    controller?.abort();
    busy.value = false;
    loading.value = false;
  }
  function update(value: ImageEditRecipe): void {
    if (locked.value) return;
    recipe.value = ImageEditRecipeExtensions.copy(value);
    clearPreview();
    error.value = '';
  }
  async function load(): Promise<void> {
    cancel();
    clearPreview();
    recipe.value = ImageEditRecipeExtensions.create();
    capabilities.value = null;
    error.value = '';
    const request = ++sequence,
      current = new AbortController();
    controller = current;
    loading.value = true;
    try {
      const result = await blogApi.editingCapabilities(current.signal);
      if (disposed || current.signal.aborted || request !== sequence) return;
      if (result.ok) capabilities.value = result.value;
      else error.value = describeFailure(result.failure);
    } catch {
      if (request === sequence && !disposed)
        error.value = 'Image editing capabilities are unavailable. Retry when ready.';
    } finally {
      if (request === sequence) loading.value = false;
    }
  }
  async function process(action: 'preview' | 'apply', intent: ImageEditRecipe): Promise<MediaView | null> {
    if (locked.value) return null;
    const original = source(),
      copied = ImageEditRecipeExtensions.copy(intent),
      fingerprint = JSON.stringify(recipe.value);
    if (JSON.stringify(copied) !== fingerprint) return null;
    const limits = capabilities.value!;
    if (
      ImageEditRecipeExtensions.validate(copied, {
        naturalWidth: original.width!,
        naturalHeight: original.height!,
        maxPixels: limits.maximumPixels,
        maxDimension: limits.maximumDimension,
        canRemoveBackground: limits.backgroundRemoval.available,
      })
    ) {
      error.value = 'Review the image dimensions and available tools before applying edits.';
      return null;
    }
    const request = ++sequence,
      current = new AbortController();
    controller?.abort();
    controller = current;
    busy.value = true;
    error.value = '';
    try {
      if (action === 'preview') {
        const result = await blogApi.previewMediaEdit(original.sha256, copied, current.signal);
        if (
          disposed ||
          current.signal.aborted ||
          request !== sequence ||
          !allowed() ||
          source().sha256 !== original.sha256 ||
          JSON.stringify(recipe.value) !== fingerprint
        )
          return null;
        if (!result.ok) error.value = describeFailure(result.failure);
        else {
          clearPreview();
          previewUrl.value = URL.createObjectURL(result.value.blob);
        }
        return null;
      }
      const result = await blogApi.applyMediaEdit(original.sha256, copied, current.signal);
      if (
        disposed ||
        current.signal.aborted ||
        request !== sequence ||
        !allowed() ||
        source().sha256 !== original.sha256 ||
        JSON.stringify(recipe.value) !== fingerprint
      )
        return null;
      if (!result.ok) {
        error.value = describeFailure(result.failure);
        return null;
      }
      return result.value;
    } catch {
      if (request === sequence && !disposed)
        error.value = 'Image editing did not finish. Your cover is unchanged. Retry when ready.';
      return null;
    } finally {
      if (request === sequence) busy.value = false;
    }
  }
  watch(
    () => source().sha256,
    () => {
      void load();
    },
    { immediate: true },
  );
  watch(allowed, (value) => {
    if (!value) cancel();
    else if (!capabilities.value && !loading.value) void load();
  });
  onScopeDispose(() => {
    disposed = true;
    cancel();
    clearPreview();
  });
  return {
    recipe,
    capabilities,
    previewUrl,
    error,
    busy,
    loading,
    sourceReady,
    locked,
    update,
    load,
    preview: (intent: ImageEditRecipe) => process('preview', intent),
    apply: (intent: ImageEditRecipe) => process('apply', intent),
  };
}
