<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, ref } from 'vue';
import type { AssetModel } from '../../domain/marketing';
const { t: ui } = useUiLocale();

const props = defineProps<{ assets: AssetModel[]; modelValue: string[]; productAssetIds: string[] }>();
const emit = defineEmits<{ 'update:modelValue': [string[]] }>();
const all = ref(false);
const visible = computed(() =>
  props.assets.filter((a) => all.value || props.productAssetIds.includes(a.id) || props.modelValue.includes(a.id)),
);
function toggle(id: string) {
  emit(
    'update:modelValue',
    props.modelValue.includes(id)
      ? props.modelValue.filter((value) => value !== id)
      : props.modelValue.length < 10
        ? [...props.modelValue, id]
        : props.modelValue,
  );
}
</script>
<template>
  <details class="rounded-xl border border-border p-4">
    <summary class="cursor-pointer text-xs font-semibold">
      {{ ui('Choose library image ·') }} {{ modelValue.length }}/10
    </summary>
    <p class="mt-3 text-xs leading-5 text-muted-foreground">
      {{ ui('Choose a product photo, uploaded image or prepared card. Your selection order is preserved.') }}
    </p>
    <label class="mt-3 flex gap-2 text-xs"><input v-model="all" type="checkbox" />{{ ui('All images') }}</label>
    <div class="mt-3 grid max-h-80 grid-cols-2 gap-2 overflow-y-auto">
      <button
        v-for="asset in visible"
        :key="asset.id"
        type="button"
        :aria-pressed="modelValue.includes(asset.id)"
        :aria-label="asset.fileName"
        :disabled="modelValue.length >= 10 && !modelValue.includes(asset.id)"
        class="min-w-0 rounded-lg border p-2 text-left"
        :class="modelValue.includes(asset.id) ? 'border-primary bg-primary-soft' : 'border-border'"
        @click="toggle(asset.id)"
      >
        <img :src="asset.url" :alt="asset.fileName" class="h-24 w-full object-contain" loading="lazy" />
        <p class="mt-1 truncate text-[10px]">
          {{ modelValue.includes(asset.id) ? `${modelValue.indexOf(asset.id) + 1}. ` : '' }}{{ asset.fileName }}
        </p>
        <p class="text-[10px] text-muted-foreground">{{ ui(asset.role) }}</p>
      </button>
    </div>
  </details>
</template>
