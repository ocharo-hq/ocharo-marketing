<script setup lang="ts">
import MarketingSelect from "./MarketingSelect.vue";
import { useUiLocale } from "./UseUiLocale";
const { uiLocale, setUiLocale, t } = useUiLocale();
</script>
<template>
  <label
    class="flex min-w-0 items-center gap-2 text-xs text-muted-foreground"
    :title="t('Content language is independent of the interface.')"
  >
    <span class="sr-only sm:not-sr-only">{{ t("Interface language") }}</span>
    <MarketingSelect
      class="max-w-40"
      :model-value="uiLocale"
      :label="t('Interface language')"
      :options="[
        { value: 'uz' as const, label: 'O‘zbekcha' },
        { value: 'ru' as const, label: 'Русский' },
        { value: 'en' as const, label: 'English' },
      ]"
      @update:model-value="setUiLocale"
    />
  </label>
</template>
