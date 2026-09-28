<script setup lang="ts">
import { computed } from 'vue';
import { ArrowUpRight, BarChart3, Link2 } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import Button from './compatibility/ActionButton.vue';
import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions } from '../../domain/marketing';
import StatusIndicator from './StatusIndicator.vue';
import ObservedMetricsPanel from './ObservedMetricsPanel.vue';

/** Presents observations only when actual channel evidence exists. */
defineOptions({ name: 'ResultsPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
/** Published entries are separate from planned posts. */
const published = computed(
  () => workspace.value?.schedules.filter((item) => ['published', 'accepted', 'unknown'].includes(item.status)) ?? [],
);
/** Metric labels use familiar operator language. */
function metricName(name: string) {
  return (
    (
      {
        views: 'Ko‘rishlar',
        reach: 'Qamrov',
        engagement: 'Faollik',
        followers: 'Obunachilar',
        likes: 'Yoqtirishlar',
        saves: 'Saqlashlar',
        comments: 'Izohlar',
      } as Record<string, string>
    )[name] ?? name
  );
}
</script>

<template>
  <div class="space-y-7">
    <div class="flex flex-wrap items-start justify-between gap-5">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">Natijalar</p>
        <h1 class="mt-2 font-editorial text-3xl">Nima odamlarni qiziqtirdi?</h1>
        <p class="mt-3 max-w-xl text-sm leading-7 text-muted-foreground">
          Ko‘rsatkichlar haqiqiy kuzatuv mavjud bo‘lganda ko‘rinadi. Yetishmayotgan ma’lumot nolga teng emas.
        </p>
      </div>
      <Button variant="outline" @click="desk.navigate('/connections')"
        ><Icon :icon="Link2" :size="16" class="mr-2" />Kanallar</Button
      >
    </div>
    <div v-if="workspace?.metrics.length && !published.length" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <article
        v-for="metric in workspace.metrics"
        :key="`${metric.channel}:${metric.name}`"
        class="rounded-2xl border border-border bg-card p-6"
      >
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs capitalize text-muted-foreground">{{ metric.channel }}</p>
            <h2 class="mt-2 text-sm font-semibold">{{ metricName(metric.name) }}</h2>
          </div>
          <StatusIndicator :status="metric.status" />
        </div>
        <p class="mt-6 font-editorial text-4xl">
          {{ metric.value === null ? '—' : new Intl.NumberFormat('uz-UZ').format(metric.value) }}
        </p>
        <p class="mt-4 text-xs leading-6 text-muted-foreground">{{ metric.explanation }}</p>
        <p v-if="metric.observedAt" class="mt-3 text-[11px] text-muted-foreground">
          Kuzatildi: {{ MarketingExtensions.formatDate(metric.observedAt, { hour: '2-digit', minute: '2-digit' }) }}
        </p>
      </article>
    </div>
    <section v-else-if="!published.length" class="rounded-2xl border border-border bg-card px-8 py-14 text-center">
      <Icon :icon="BarChart3" :size="35" class="mx-auto text-primary" />
      <h2 class="mt-5 font-editorial text-2xl">Haqiqiy natijalar uchun kanal kerak</h2>
      <p class="mx-auto mt-4 max-w-lg text-sm leading-7 text-muted-foreground">
        Hozir ko‘rishlar yoki faollik haqida ma’lumot yo‘q. Postlarni tayyorlash va qo‘lda joylash davom etishi mumkin.
      </p>
      <Button class="mt-6" @click="desk.navigate('/connections')"
        >Ulanish holatini ko‘rish <Icon :icon="ArrowUpRight" :size="16" class="ml-2"
      /></Button>
    </section>
    <section class="rounded-2xl border border-border bg-card p-6">
      <h2 class="font-editorial text-2xl">Joylash tarixi</h2>
      <div v-if="published.length" class="mt-5 divide-y divide-border">
        <div v-for="item in published" :key="item.id" class="flex flex-wrap items-center justify-between gap-4 py-4">
          <div>
            <p class="text-sm font-semibold">
              {{ item.draftTitle ?? 'Joylangan post' }}
            </p>
            <p class="mt-2 text-xs text-muted-foreground">
              {{ item.channel }} · {{ MarketingExtensions.formatDate(item.scheduledAt) }}
            </p>
            <p class="mt-2 break-all text-xs text-muted-foreground">{{ item.externalId }}</p>
          </div>
          <StatusIndicator :status="item.status" />
        </div>
      </div>
      <p v-else class="mt-4 text-sm leading-7 text-muted-foreground">
        Hali kanalga yuborish tarixi yo‘q. Rejadagi postlar bu ro‘yxatga kirmaydi.
      </p>
    </section>
    <div v-if="published.length" class="space-y-5">
      <ObservedMetricsPanel v-for="plan in published" :key="plan.id" :plan="plan" />
    </div>
    <section class="grid gap-4 md:grid-cols-3">
      <article
        v-for="idea in [
          { title: 'A yoki B', note: 'Izoh va javoblar orqali rang yoki dizayn qiziqishini baholang.' },
          { title: 'Mahsulot kartasi', note: 'Saqlash va mahsulot haqidagi savollarni kuzating.' },
          { title: 'Jarayon videosi', note: 'Ko‘rish davomiyligi va ulashishlarni kuzating.' },
        ]"
        :key="idea.title"
        class="rounded-xl border border-dashed border-border p-5"
      >
        <h3 class="text-sm font-semibold">{{ idea.title }}</h3>
        <p class="mt-3 text-xs leading-6 text-muted-foreground">{{ idea.note }}</p>
        <span class="mt-4 inline-block text-[10px] uppercase tracking-wider text-primary">Keyingi tahlil uchun</span>
      </article>
    </section>
  </div>
</template>
