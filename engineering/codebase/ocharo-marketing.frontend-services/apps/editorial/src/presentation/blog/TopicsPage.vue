<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { ref, watch } from 'vue';
import { Plus, Trash2 } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { useBlogStore, useSiteAdmin } from '../../application/blog';
import type { TopicDocument } from '../../domain/blog/BlogModels';
const { t: ui } = useUiLocale();

/** Reading sections shown in the site navigation. */
defineOptions({ name: 'TopicsPage' });
const store = useBlogStore();
const admin = useSiteAdmin();
const rows = ref<TopicDocument[]>([]);
watch(
  () => store.topics.value,
  (topics) => {
    rows.value = structuredClone(topics);
  },
  { immediate: true },
);
function add() {
  rows.value.push({ key: '', name: '', short: '', description: '', sortOrder: rows.value.length });
}
function setTranslation(topic: TopicDocument, lang: string, field: 'name' | 'short' | 'description', value: string) {
  const translations = { ...(topic.translations ?? {}) };
  const current = translations[lang] ?? { name: '', short: '', description: '' };
  translations[lang] = { ...current, [field]: value };
  topic.translations = translations;
}
async function remove(key: string) {
  if (window.confirm(ui('Bo‘lim o‘chirilsinmi? Ichida hikoya bo‘lsa, rad etiladi.'))) await admin.deleteTopic(key);
}
</script>

<template>
  <section>
    <div class="flex items-end justify-between">
      <div>
        <h1 class="font-editorial text-4xl tracking-tight">{{ ui('Topics') }}</h1>
        <p class="mt-2 text-sm text-muted-foreground">{{ ui('Site navigation and /topics/… pages.') }}</p>
      </div>
      <button class="btn-primary" @click="add"><Icon :icon="Plus" :size="15" />{{ ui('Topic') }}</button>
    </div>
    <ul class="mt-6 space-y-3">
      <li v-for="(t, index) in rows" :key="index" class="panel grid gap-3 md:grid-cols-[140px_1fr_1fr_90px_auto]">
        <div>
          <label class="label" :for="`k-${index}`">{{ ui('Key') }}</label
          ><input
            :id="`k-${index}`"
            v-model="t.key"
            class="field font-mono text-xs"
            :disabled="store.topics.value.some((x) => x.key === t.key)"
          />
        </div>
        <div>
          <label class="label" :for="`n-${index}`">{{ ui('Name') }}</label
          ><input :id="`n-${index}`" v-model="t.name" class="field" />
        </div>
        <div>
          <label class="label" :for="`s-${index}`">{{ ui('In navigation') }}</label
          ><input :id="`s-${index}`" v-model="t.short" class="field" />
        </div>
        <div>
          <label class="label" :for="`o-${index}`">{{ ui('Order') }}</label
          ><input :id="`o-${index}`" v-model.number="t.sortOrder" type="number" class="field" />
        </div>
        <div class="flex items-end gap-2">
          <button class="btn-primary" @click="admin.saveTopic(t)">{{ ui('Save') }}</button>
          <button
            v-if="store.topics.value.some((x) => x.key === t.key)"
            class="btn-ghost text-destructive"
            :aria-label="ui('Delete')"
            @click="remove(t.key)"
          >
            <Icon :icon="Trash2" :size="14" />
          </button>
        </div>
        <div class="md:col-span-5">
          <label class="label" :for="`d-${index}`">{{ ui('Description') }}</label
          ><textarea :id="`d-${index}`" v-model="t.description" rows="2" class="field"></textarea>
        </div>
        <div
          v-for="lang in ['uz', 'ru']"
          :key="lang"
          class="grid gap-3 rounded-xl bg-muted/50 p-3 md:col-span-5 md:grid-cols-[1fr_160px_2fr]"
        >
          <div>
            <label class="label" :for="`${lang}-n-${index}`">{{ ui('Name (') }}{{ lang }})</label>
            <input
              :id="`${lang}-n-${index}`"
              :value="t.translations?.[lang]?.name ?? ''"
              class="field"
              @input="setTranslation(t, lang, 'name', ($event.target as HTMLInputElement).value)"
            />
          </div>
          <div>
            <label class="label" :for="`${lang}-s-${index}`">{{ ui('In navigation (') }}{{ lang }})</label>
            <input
              :id="`${lang}-s-${index}`"
              :value="t.translations?.[lang]?.short ?? ''"
              class="field"
              @input="setTranslation(t, lang, 'short', ($event.target as HTMLInputElement).value)"
            />
          </div>
          <div>
            <label class="label" :for="`${lang}-d-${index}`">{{ ui('Description (') }}{{ lang }})</label>
            <input
              :id="`${lang}-d-${index}`"
              :value="t.translations?.[lang]?.description ?? ''"
              class="field"
              @input="setTranslation(t, lang, 'description', ($event.target as HTMLInputElement).value)"
            />
          </div>
        </div>
      </li>
    </ul>
  </section>
</template>
