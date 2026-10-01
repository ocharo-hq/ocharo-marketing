<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { Download, ExternalLink, Plus, Trash2, Upload } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { useArticleList, useBlogStore, useSiteAdmin } from '../../application/blog';

/** Site identity, launch switch (noindex), footer links, home imagery and static export. */
defineOptions({ name: 'SettingsPage' });
const store = useBlogStore();
const admin = useSiteAdmin();
const stories = useArticleList();
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
  <section v-if="admin.settings.value" class="space-y-6">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <h1 class="font-editorial text-4xl tracking-tight">Sayt</h1>
      <div class="flex gap-2">
        <a href="/" target="_blank" rel="noopener" class="btn-outline"
          ><Icon :icon="ExternalLink" :size="15" />Saytni ochish</a
        >
        <a :href="store.links.export" class="btn-outline" download
          ><Icon :icon="Download" :size="15" />Statik sayt (ZIP)</a
        >
      </div>
    </div>

    <section class="panel" aria-labelledby="launch">
      <h2 id="launch" class="font-editorial text-2xl">Ishga tushirish</h2>
      <label class="mt-4 flex items-start gap-3 text-sm">
        <input v-model="admin.settings.value.noIndex" type="checkbox" class="mt-1" />
        <span
          ><strong>Qidiruv tizimlaridan yashirish (noindex)</strong><br /><span class="text-muted-foreground"
            >Rasmiy ochilishgacha yoqilgan qoladi. O‘chirsangiz, robots.txt va sahifalar indekslashga ruxsat
            beradi.</span
          ></span
        >
      </label>
      <div class="mt-4 grid gap-4 md:grid-cols-2">
        <div>
          <label class="label" for="base">Asosiy manzil (RSS, sitemap uchun)</label
          ><input id="base" v-model="admin.settings.value.baseUrl" class="field" placeholder="https://blog.ocharo.uz" />
        </div>
        <div>
          <label class="label" for="featured">Bosh sahifadagi asosiy hikoya</label>
          <select id="featured" v-model="admin.settings.value.featuredSlug" class="field">
            <option :value="null">Eng so‘nggisi</option>
            <option v-for="s in stories.articles.value" :key="s.slug" :value="s.slug">{{ s.title }}</option>
          </select>
        </div>
      </div>
    </section>

    <section class="panel" aria-labelledby="identity">
      <h2 id="identity" class="font-editorial text-2xl">Matnlar</h2>
      <div class="mt-4 grid gap-4 md:grid-cols-2">
        <div>
          <label class="label" for="t">Sayt nomi</label
          ><input id="t" v-model="admin.settings.value.title" class="field" />
        </div>
        <div>
          <label class="label" for="e">Ustki qator</label
          ><input id="e" v-model="admin.settings.value.eyebrow" class="field" />
        </div>
        <div class="md:col-span-2">
          <label class="label" for="m">Bosh sahifa tavsifi</label
          ><input id="m" v-model="admin.settings.value.mastheadDescription" class="field" />
        </div>
        <div>
          <label class="label" for="ht">Bosh sahifa sarlavhasi (title)</label
          ><input id="ht" v-model="admin.settings.value.homeTitle" class="field" />
        </div>
        <div>
          <label class="label" for="hd">Meta tavsif</label
          ><input id="hd" v-model="admin.settings.value.homeDescription" class="field" />
        </div>
        <div>
          <label class="label" for="ft">Pastki qator</label
          ><input id="ft" v-model="admin.settings.value.footerTagline" class="field" />
        </div>
        <div>
          <label class="label" for="en">Nashr izohi</label
          ><input id="en" v-model="admin.settings.value.editionNote" class="field" />
        </div>
      </div>
      <h3 class="mt-6 text-sm font-semibold">Pastki havolalar</h3>
      <p v-if="admin.brand.value?.connected" class="mt-1 text-xs text-muted-foreground" role="note">
        Brend markazi ulangan (nashr {{ admin.brand.value.release }}): saytda logotip va havolalar o‘sha yerdan olinadi.
        Bu yerdagi qiymatlar faqat Brend markazi ishlamay qolganda ishlatiladi.
      </p>
      <ul class="mt-2 space-y-2">
        <li
          v-for="(link, index) in admin.settings.value.links"
          :key="index"
          class="grid gap-2 sm:grid-cols-[200px_1fr_auto]"
        >
          <input v-model="link.label" class="field" aria-label="Havola nomi" />
          <input v-model="link.url" class="field font-mono text-xs" aria-label="Havola manzili" />
          <button
            class="btn-ghost text-destructive"
            aria-label="O‘chirish"
            @click="admin.settings.value.links.splice(index, 1)"
          >
            <Icon :icon="Trash2" :size="14" />
          </button>
        </li>
      </ul>
      <button class="btn-outline mt-2" @click="admin.settings.value.links.push({ label: '', url: 'https://' })">
        <Icon :icon="Plus" :size="14" />Havola
      </button>
    </section>

    <section class="panel" aria-labelledby="images">
      <div class="flex items-center justify-between">
        <h2 id="images" class="font-editorial text-2xl">Rasmlar</h2>
        <button class="btn-outline" @click="uploadInput?.click()"><Icon :icon="Upload" :size="14" />Yuklash</button>
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
          <label class="label" :for="slot.key">{{ slot.label }}</label>
          <select :id="slot.key" v-model="admin.settings.value[slot.key]" class="field">
            <option :value="null">—</option>
            <option v-for="m in admin.media.value" :key="m.sha256" :value="m.sha256">{{ m.fileName }}</option>
          </select>
          <img
            v-if="admin.settings.value[slot.key]"
            :src="store.links.media(admin.settings.value[slot.key]!)"
            alt=""
            class="mt-2 max-h-32 rounded-lg border border-border object-contain"
          />
        </div>
      </div>
    </section>

    <div class="flex justify-end">
      <button class="btn-primary" :disabled="admin.busy.value" @click="admin.save()">Saqlash</button>
    </div>
  </section>
  <p v-else class="text-sm text-muted-foreground" role="status">Yuklanmoqda…</p>
</template>
