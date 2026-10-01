<script setup lang="ts">
import { useUiLocale } from "./UseUiLocale";
const { uiLocale, setUiLocale, t } = useUiLocale();
</script>
<template>
  <label
    class="flex min-w-0 items-center gap-2 text-xs text-muted-foreground"
    :title="t('Content language is independent of the interface.')"
  >
    <span class="sr-only sm:not-sr-only">{{ t("Interface language") }}</span>
    <select
      class="min-h-9 max-w-32 rounded-lg border border-border bg-card px-2 py-1 text-foreground"
      :value="uiLocale"
      :aria-label="t('Interface language')"
      @change="setUiLocale(($event.target as HTMLSelectElement).value)"
    >
      <option value="uz">O‘zbekcha</option>
      <option value="ru">Русский</option>
      <option value="en">English</option>
    </select>
  </label>
</template>
