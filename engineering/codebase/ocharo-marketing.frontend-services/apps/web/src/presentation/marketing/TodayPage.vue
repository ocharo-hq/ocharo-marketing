<script setup lang="ts">
import { computed } from 'vue';
import { ArrowRight, CalendarDays, Check, ImagePlus, Plus, Sparkles } from 'lucide-vue-next';

import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import Button from './compatibility/ActionButton.vue';

import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions } from '../../domain/marketing';
import CardPreview from './CardPreview.vue';
import StatusIndicator from './StatusIndicator.vue';

/** Presents actual preparation work and the next useful post action. */
defineOptions({ name: 'TodayPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
/** The latest editable post remains the primary continuation target. */
const recent = computed(
  () => [...(workspace.value?.drafts ?? [])].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt))[0],
);
/** The preview uses the current post product or first imported product. */
const product = computed(
  () => workspace.value?.products.find((item) => item.id === recent.value?.productId) ?? workspace.value?.products[0],
);
/** The preview keeps front view and the saved preset. */
const composition = computed(() => ({
  productId: product.value?.id ?? '',
  presetId: recent.value?.presetId ?? 't02-bone',
  title: recent.value?.composition?.title ?? recent.value?.title ?? product.value?.name ?? '',
  subtitle: recent.value?.composition?.subtitle ?? '',
  fabricAssetId: recent.value?.composition?.fabricAssetId ?? undefined,
  scale: recent.value?.composition?.scale ?? 1,
  offsetX: recent.value?.composition?.offsetX ?? 0,
  offsetY: recent.value?.composition?.offsetY ?? 0,
  locale: recent.value?.locale ?? 'uz',
}));
/** The saved exact artwork takes precedence over regenerating a proof. */
const selectedArtwork = computed(() => workspace.value?.assets.find((asset) => asset.id === recent.value?.mediaIds[0]));
/** The counts concern persisted work, not audience response. */
const counts = computed(() => [
  {
    label: 'Qoralama',
    value: workspace.value?.drafts.filter((item) => item.status === 'draft').length ?? 0,
    icon: ImagePlus,
  },
  {
    label: 'Tekshirilgan',
    value: workspace.value?.drafts.filter((item) => item.status === 'approved').length ?? 0,
    icon: Check,
  },
  {
    label: 'Rejada',
    value: workspace.value?.schedules.filter((item) => item.status === 'planned').length ?? 0,
    icon: CalendarDays,
  },
]);
/** The upcoming items retain their recorded scheduling state. */
const upcoming = computed(() =>
  [...(workspace.value?.schedules ?? [])]
    .filter((item) => item.status !== 'cancelled')
    .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt))
    .slice(0, 3),
);
</script>

<template>
  <div class="space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="mb-2 text-xs font-semibold uppercase tracking-widest text-primary">Ocharo · Marketing</p>
        <h1 class="font-editorial text-3xl tracking-tight md:text-4xl">Postingizga shakl bering.</h1>
        <p class="mt-3 max-w-xl text-sm leading-relaxed text-muted-foreground">
          Mahsulot rasmlari, tayyor shablonlar va bir joyda saqlangan rejangiz.
        </p>
      </div>
      <Button @click="desk.navigate('/posts/new')"><Icon :icon="Plus" :size="17" class="mr-2" />Post yaratish</Button>
    </div>

    <section
      class="grid overflow-hidden rounded-2xl border border-border bg-card shadow-paper lg:grid-cols-[1.05fr_1fr]"
    >
      <div class="flex flex-col justify-between p-6 md:p-9">
        <div>
          <span class="rounded-full bg-primary-soft px-3 py-1 text-xs font-semibold text-primary">{{
            recent ? 'Davom ettiring' : 'Birinchi post'
          }}</span>
          <h2 class="mt-7 max-w-sm font-editorial text-3xl leading-tight md:text-4xl">
            {{ recent?.title ?? 'Mahsulot — diqqat markazida.' }}
          </h2>
          <p class="mt-4 max-w-sm text-sm leading-7 text-muted-foreground">
            {{
              recent
                ? 'Saqlangan postingizni ochib, matn va ko‘rinishini tekshiring.'
                : 'Old ko‘rinishni tanlang, shablonni qo‘llang va postingizni tayyorlang.'
            }}
          </p>
          <div class="mt-6 flex flex-wrap gap-2">
            <span class="rounded-lg bg-muted px-3 py-2 text-xs">Old ko‘rinish</span
            ><span class="rounded-lg bg-muted px-3 py-2 text-xs">4:5 format</span
            ><span class="rounded-lg bg-muted px-3 py-2 text-xs">O‘zbekcha</span>
          </div>
        </div>
        <div class="mt-9">
          <Button
            variant="outline"
            @click="desk.navigate(recent ? `/posts/${recent.id}` : `/posts/new?product=${product?.id ?? ''}`)"
            >{{ recent ? 'Postni ochish' : 'Shu mahsulotdan boshlash'
            }}<Icon :icon="ArrowRight" :size="16" class="ml-2"
          /></Button>
          <p class="mt-4 text-xs text-muted-foreground">Shablon va matnni tekshirish post yaratishning bir qismidir.</p>
        </div>
      </div>
      <div class="flex items-center justify-center border-t border-border bg-muted p-6 lg:border-l lg:border-t-0">
        <div class="w-full max-w-64 rotate-1">
          <img
            v-if="selectedArtwork"
            :src="selectedArtwork.url"
            alt="Saqlangan postning aynan tanlangan tasviri"
            class="aspect-[4/5] w-full rounded-xl object-contain"
          /><CardPreview v-else :composition="composition" :show-issues="false" />
        </div>
      </div>
    </section>

    <div class="grid gap-4 sm:grid-cols-3">
      <button
        v-for="count in counts"
        :key="count.label"
        class="flex items-center gap-4 rounded-xl border border-border bg-card p-5 text-left transition hover:border-primary"
        @click="desk.navigate(count.label === 'Rejada' ? '/calendar' : '/posts')"
      >
        <span class="flex size-10 items-center justify-center rounded-xl bg-muted text-primary"
          ><Icon :icon="count.icon" :size="19"
        /></span>
        <div>
          <p class="text-2xl font-semibold">{{ count.value }}</p>
          <p class="mt-1 text-xs text-muted-foreground">{{ count.label }}</p>
        </div>
      </button>
    </div>

    <div class="grid gap-6 xl:grid-cols-[1.5fr_1fr]">
      <section class="rounded-2xl border border-border bg-card p-6">
        <div class="mb-5 flex items-center justify-between">
          <h2 class="font-editorial text-xl">Mahsulotdan postgacha</h2>
          <button class="text-xs font-semibold text-primary" @click="desk.navigate('/products')">
            Barchasi <span aria-hidden="true">→</span>
          </button>
        </div>
        <div class="grid grid-cols-3 gap-3">
          <button
            v-for="item in workspace?.products.slice(0, 3)"
            :key="item.id"
            class="group text-left"
            @click="desk.navigate(`/posts/new?product=${item.id}`)"
          >
            <div class="flex aspect-[4/5] items-center justify-center overflow-hidden rounded-xl bg-muted p-3">
              <img
                v-if="MarketingExtensions.productAsset(workspace, item)"
                :src="MarketingExtensions.productAsset(workspace, item) ?? ''"
                :alt="item.name"
                class="h-full w-full object-contain transition group-hover:scale-105"
              />
            </div>
            <p class="mt-3 text-sm font-semibold">{{ item.name }}</p>
            <p class="mt-1 text-xs text-muted-foreground">{{ item.category }}</p>
          </button>
        </div>
      </section>
      <section class="rounded-2xl border border-border bg-card p-6">
        <div class="mb-5 flex items-center justify-between">
          <h2 class="font-editorial text-xl">Yaqin reja</h2>
          <Icon :icon="CalendarDays" :size="18" class="text-muted-foreground" />
        </div>
        <div
          v-if="!upcoming.length"
          class="flex min-h-48 flex-col items-center justify-center rounded-xl border border-dashed border-border p-5 text-center"
        >
          <p class="text-sm font-semibold">Rejangiz hali bo‘sh</p>
          <p class="mt-2 text-xs leading-6 text-muted-foreground">
            Tekshirilgan postni rejangizga qo‘shing. Ulanmagan kanalda u reja sifatida saqlanadi.
          </p>
          <Button variant="ghost" size="sm" class="mt-4" @click="desk.navigate('/calendar')">Rejani ochish</Button>
        </div>
        <div v-else class="space-y-4">
          <div v-for="item in upcoming" :key="item.id" class="border-b border-border pb-4 last:border-b-0">
            <p class="text-xs text-muted-foreground">
              {{ MarketingExtensions.formatDate(item.scheduledAt, { hour: '2-digit', minute: '2-digit' }) }}
            </p>
            <p class="my-2 text-sm font-semibold">
              {{
                item.draftTitle ??
                workspace?.drafts.find((post) => post.id === item.draftId && post.revision === item.draftRevision)
                  ?.title ??
                'Saqlangan post'
              }}
            </p>
            <StatusIndicator :status="item.status" />
          </div>
        </div>
      </section>
    </div>
    <section class="flex flex-wrap items-center justify-between gap-4 rounded-xl bg-primary-soft px-6 py-5">
      <div class="flex items-center gap-3">
        <Icon :icon="Sparkles" :size="19" class="text-primary" />
        <div>
          <h2 class="text-sm font-semibold">Shablonlarni birga tanlang</h2>
          <p class="mt-1 text-xs text-muted-foreground">
            Atlasda taqqoslang; kundalik ishda tayyor variantni qo‘llang.
          </p>
        </div>
      </div>
      <Button variant="ghost" size="sm" @click="desk.navigate('/atlas')"
        >Atlasni ko‘rish <Icon :icon="ArrowRight" :size="15" class="ml-2"
      /></Button>
    </section>
  </div>
</template>
