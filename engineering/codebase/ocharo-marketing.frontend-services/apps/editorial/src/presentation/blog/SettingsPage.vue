<script setup lang="ts">
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { onMounted, ref } from 'vue';
import { Download, ExternalLink, Plus, Trash2, Upload } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { useArticleList, useBlogStore, useSiteAdmin } from '../../application/blog';
import { useRefresh } from '@wow-two-beta/fe/query';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import EditorSkeleton from './components/EditorSkeleton.vue';
const { t: ui } = useUiLocale();

/** Site identity, launch switch (noindex), footer links, home imagery and static export. */
defineOptions({ name: 'SettingsPage' });
const store = useBlogStore();
const admin = useSiteAdmin();
const stories = useArticleList();
const { refresh, refreshing } = useRefresh(() => admin.load());
const uploadInput = ref<HTMLInputElement | null>(null);
async function onUpload(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (file) await admin.upload(file);
  if (uploadInput.value) uploadInput.value.value = '';
}
onMounted(async () => {
  await admin.load();
  stories.filter.status = 'published';
  await stories.load();
});
</script>

<template>
  <div class="mb-6 flex flex-wrap items-end justify-between gap-4">
    <h1 class="font-editorial text-4xl tracking-tight">{{ ui('Site') }}</h1>
    <div class="flex gap-2">
      <Button
        variant="outline"
        :is-disabled="admin.busy.value || admin.loading.value || refreshing || admin.dirty.value"
        :is-loading="refreshing"
        @click="refresh()"
        >{{ ui('Refresh') }}</Button
      ><a href="/" target="_blank" rel="noopener" class="btn-outline"
        ><Icon :icon="ExternalLink" :size="15" />{{ ui('Open site') }}</a
      ><a :href="store.links.export" class="btn-outline" download
        ><Icon :icon="Download" :size="15" />{{ ui('Static site (ZIP)') }}</a
      >
    </div>
  </div>
  <EditorSkeleton v-if="admin.loading.value && !admin.settings.value" kind="settings" />
  <section v-if="admin.settings.value" :aria-busy="admin.loading.value || refreshing" class="space-y-6">
    <fieldset :disabled="admin.busy.value || admin.loading.value || refreshing" class="space-y-6 min-w-0 border-0 p-0">
      <section class="panel" aria-labelledby="launch">
        <h2 id="launch" class="font-editorial text-2xl">{{ ui('Run') }}</h2>
        <label class="mt-4 flex items-start gap-3 text-sm">
          <input v-model="admin.settings.value.noIndex" type="checkbox" class="mt-1" />
          <span
            ><strong>{{ ui('Hide from search engines (noindex)') }}</strong
            ><br /><span class="text-muted-foreground">{{
              ui('Keep enabled until the official launch. Turning it off allows indexing in robots.txt and pages.')
            }}</span></span
          >
        </label>
        <div class="mt-4 grid gap-4 md:grid-cols-2">
          <div>
            <label class="label" for="base">{{ ui('Base URL (for RSS and sitemap)') }}</label
            ><input
              id="base"
              v-model="admin.settings.value.baseUrl"
              class="field"
              placeholder="https://blog.ocharo.uz"
            />
          </div>
          <div>
            <label class="label" for="featured">{{ ui('Featured homepage story') }}</label>
            <MarketingSelect
              id="featured"
              :model-value="admin.settings.value.featuredSlug ?? ''"
              :label="ui('Featured homepage story')"
              :options="[
                { value: '' as const, label: ui('Latest') },
                ...stories.articles.value.map((s) => ({ value: s.slug, label: s.title })),
              ]"
              :disabled="admin.busy.value || admin.loading.value || refreshing"
              @update:model-value="admin.settings.value.featuredSlug = $event || null"
            />
          </div>
        </div>
      </section>

      <section class="panel" aria-labelledby="identity">
        <h2 id="identity" class="font-editorial text-2xl">{{ ui('Texts') }}</h2>
        <div class="mt-4 grid gap-4 md:grid-cols-2">
          <div>
            <label class="label" for="t">{{ ui('Site name') }}</label
            ><input id="t" v-model="admin.settings.value.title" class="field" />
          </div>
          <div>
            <label class="label" for="e">{{ ui('Top line') }}</label
            ><input id="e" v-model="admin.settings.value.eyebrow" class="field" />
          </div>
          <div class="md:col-span-2">
            <label class="label" for="m">{{ ui('Homepage description') }}</label
            ><input id="m" v-model="admin.settings.value.mastheadDescription" class="field" />
          </div>
          <div>
            <label class="label" for="ht">{{ ui('Homepage title') }}</label
            ><input id="ht" v-model="admin.settings.value.homeTitle" class="field" />
          </div>
          <div>
            <label class="label" for="hd">{{ ui('Meta description') }}</label
            ><input id="hd" v-model="admin.settings.value.homeDescription" class="field" />
          </div>
          <div>
            <label class="label" for="ft">{{ ui('Bottom line') }}</label
            ><input id="ft" v-model="admin.settings.value.footerTagline" class="field" />
          </div>
          <div>
            <label class="label" for="en">{{ ui('Publication note') }}</label
            ><input id="en" v-model="admin.settings.value.editionNote" class="field" />
          </div>
        </div>
        <h3 class="mt-6 text-sm font-semibold">{{ ui('Footer links') }}</h3>
        <p v-if="admin.brand.value?.connected" class="mt-1 text-xs text-muted-foreground" role="note">
          {{ ui('Brand hub connected (revision') }} {{ admin.brand.value.release
          }}{{
            ui('): the site uses its logo and links. These values are used only when the Brand hub is unavailable.')
          }}
        </p>
        <ul class="mt-2 space-y-2">
          <li
            v-for="(link, index) in admin.settings.value.links"
            :key="index"
            class="grid gap-2 sm:grid-cols-[200px_1fr_auto]"
          >
            <input v-model="link.label" class="field" :aria-label="ui('Link label')" />
            <input v-model="link.url" class="field font-mono text-xs" :aria-label="ui('Link URL')" />
            <button
              class="btn-ghost text-destructive"
              :aria-label="ui('Delete')"
              @click="admin.settings.value.links.splice(index, 1)"
            >
              <Icon :icon="Trash2" :size="14" />
            </button>
          </li>
        </ul>
        <button class="btn-outline mt-2" @click="admin.settings.value.links.push({ label: '', url: 'https://' })">
          <Icon :icon="Plus" :size="14" />{{ ui('Link') }}
        </button>
      </section>

      <section class="panel" aria-labelledby="images">
        <div class="flex items-center justify-between">
          <h2 id="images" class="font-editorial text-2xl">{{ ui('Images') }}</h2>
          <button class="btn-outline" @click="uploadInput?.click()">
            <Icon :icon="Upload" :size="14" />{{ ui('Download') }}
          </button>
          <input
            ref="uploadInput"
            type="file"
            accept="image/png,image/jpeg,image/webp"
            class="hidden"
            @change="onUpload"
          />
        </div>
        <div class="mt-4 grid gap-4 md:grid-cols-3">
          <div
            v-for="slot in [
              { key: 'logoSha256', label: 'Logotip' },
              { key: 'heroSha256', label: 'Bosh rasm' },
              { key: 'bandSha256', label: 'Keng lenta' },
            ] as const"
            :key="slot.key"
          >
            <label class="label" :for="slot.key">{{ ui(slot.label) }}</label>
            <MarketingSelect
              :id="slot.key"
              :model-value="admin.settings.value[slot.key] ?? ''"
              :label="ui(slot.label)"
              :options="[
                { value: '' as const, label: '—' },
                ...admin.media.value.map((m) => ({ value: m.sha256, label: m.fileName })),
              ]"
              :disabled="admin.busy.value || admin.loading.value || refreshing"
              @update:model-value="admin.settings.value[slot.key] = $event || null"
            />
            <img
              v-if="admin.settings.value[slot.key]"
              :src="store.links.media(admin.settings.value[slot.key]!)"
              alt=""
              class="mt-2 max-h-32 rounded-lg border border-border object-contain"
            />
          </div>
        </div>
      </section>
    </fieldset>
    <div class="flex justify-end">
      <button
        class="btn-primary"
        :disabled="admin.busy.value || admin.loading.value || refreshing"
        @click="admin.save()"
      >
        {{ ui('Save') }}
      </button>
    </div>
  </section>
  <p v-else-if="!admin.loading.value" class="query-label" role="status">
    {{ ui('Content unavailable. Try refreshing.') }}
  </p>
</template>
