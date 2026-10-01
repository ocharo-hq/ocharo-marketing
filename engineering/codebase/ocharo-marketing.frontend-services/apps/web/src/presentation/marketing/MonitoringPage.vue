<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type OperationsOverviewModel } from '../../domain/marketing';
import { OperationsExtensions as ops } from '../../domain/marketing/OperationsExtensions';
import DeliveryPanel from './DeliveryPanel.vue';
const desk = useMarketingWorkspace();
const overview = ref<OperationsOverviewModel | null>(null);
const refreshError = ref('');
const loading = ref(false);
let timer: ReturnType<typeof setTimeout> | undefined;
const abort = new AbortController();
let disposed = false;
function date(value: string | null) {
  return value ? MarketingExtensions.formatDate(value, { hour: '2-digit', minute: '2-digit' }) : 'Kuzatilmagan';
}
async function load() {
  if (loading.value || disposed) return;
  if (timer) clearTimeout(timer);
  loading.value = true;
  const result = await desk.getOperations(abort.signal);
  if (disposed) return;
  if (result.ok) {
    overview.value = result.value;
    refreshError.value = '';
  } else if (result.failure.code !== 'cancelled') refreshError.value = desk.failureMessage(result.failure);
  loading.value = false;
  // Read-only local state. No provider refresh, retry or delivery can result from this timer.
  if (!disposed) timer = setTimeout(load, 30000);
}
const workerNames: Record<string, string> = {
  automation: 'Katalog va rejalashtirish',
  publication: 'Yuborish navbati',
  generation: 'Tasvir tayyorlash',
};
const workerStatus: Record<string, string> = {
  unobserved: 'Hali kuzatilmagan',
  healthy: 'Tekshiruv ishlayapti',
  running: 'Ishga tushdi',
  failed: 'Oxirgi tekshiruv xato',
  stale: 'Yangi signal yo‘q',
};
onMounted(load);
onBeforeUnmount(() => {
  disposed = true;
  abort.abort();
  if (timer) clearTimeout(timer);
});
</script>
<template>
  <div class="space-y-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">Monitoring</p>
        <h1 class="mt-2 font-editorial text-3xl">Postlar qayerda to‘xtadi?</h1>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground">
          Mahalliy holat har 30 soniyada yangilanadi. Tasdiqlanmagan reja yuborilmaydi; noma’lum yuborish avtomatik
          takrorlanmaydi.
        </p>
      </div>
      <Button variant="outline" :is-disabled="loading" @click="load">Holatni yangilash</Button>
    </header>
    <p v-if="refreshError" role="alert" class="rounded-xl bg-warning-soft p-4 text-sm">
      {{ refreshError }} Avvalgi holat quyida saqlangan.
    </p>
    <template v-if="overview">
      <p class="text-xs text-muted-foreground">
        Saqlangan holat: {{ date(overview.observedAt) }} · {{ overview.timeZone }}
      </p>
      <section class="grid grid-cols-2 gap-3 xl:grid-cols-6">
        <article
          v-for="stat in [
            ['Tasdiqlangan, rejasiz', overview.approvedUnplanned],
            ['Navbatda', overview.queued],
            ['Yuborilmoqda', overview.running],
            ['Xato', overview.failed],
            ['Noma’lum', overview.unknown],
            ['To‘xtatilgan', overview.held],
          ]"
          :key="String(stat[0])"
          class="rounded-xl border border-border bg-card p-4"
        >
          <p class="text-xs text-muted-foreground">{{ stat[0] }}</p>
          <p class="mt-3 font-editorial text-3xl">{{ stat[1] }}</p>
        </article>
      </section>
      <section class="grid gap-4 md:grid-cols-3">
        <article
          v-for="worker in overview.workers"
          :key="worker.worker"
          class="rounded-xl border border-border bg-card p-5"
        >
          <h2 class="font-semibold">{{ workerNames[worker.worker] }}</h2>
          <p
            class="mt-3 text-sm"
            :class="['failed', 'stale', 'unobserved'].includes(worker.status) ? 'text-warning' : 'text-primary'"
          >
            {{ workerStatus[worker.status] ?? worker.status }}
          </p>
          <p class="mt-3 text-xs text-muted-foreground">Signal: {{ date(worker.checkedAt) }}</p>
          <p class="mt-1 text-xs text-muted-foreground">Muvaffaqiyatli tekshiruv: {{ date(worker.lastSuccessAt) }}</p>
          <p v-if="worker.lastError" class="mt-3 text-xs text-warning">{{ worker.lastError }}</p>
        </article>
      </section>
      <section class="rounded-xl border border-border bg-card p-5">
        <h2 class="font-editorial text-2xl">Yuborish ruxsatlari</h2>
        <p class="mt-3 text-sm">
          Jonli yuborish: {{ overview.delivery.publishingEnabled ? 'yoqilgan' : 'o‘chiq' }} · Kanal ko‘rsatkichlarini
          o‘qish: {{ overview.delivery.metricReadsEnabled ? 'yoqilgan' : 'o‘chiq' }}
        </p>
        <p class="mt-3 text-xs leading-6 text-muted-foreground">
          Ishlayotgan navbat xizmati kanalga ulanganlikni anglatmaydi. Har bir yuborish tasdiqlangan post, manzil va
          alohida ruxsat talab qiladi.
        </p>
        <p class="mt-3 text-xs">
          Katalog urinishi: {{ date(overview.automation.lastSyncAt) }} · Rejalashtirish urinishi:
          {{ date(overview.automation.lastFillAt) }}
        </p>
        <p v-if="overview.automation.lastError" class="mt-3 text-xs text-warning">
          Saqlangan oxirgi xato ({{ date(overview.automation.lastErrorAt) }}): {{ overview.automation.lastError }}
        </p>
        <Button class="mt-4" variant="outline" size="sm" @click="desk.navigate('/connections')"
          >Ulanishlarni tekshirish</Button
        >
      </section>
      <section class="rounded-2xl border border-border bg-card p-5">
        <h2 class="font-editorial text-2xl">E’tibor talab qiladigan yuborishlar</h2>
        <p v-if="!overview.attentionDeliveries.length" class="mt-4 text-sm text-muted-foreground">
          Xato, noma’lum yoki kechikkan tasdiqlangan yuborish yo‘q.
        </p>
        <article
          v-for="item in overview.attentionDeliveries"
          :key="item.id"
          class="mt-4 rounded-xl border border-border p-4"
        >
          <h3 class="text-sm font-semibold">
            {{ item.snapshot.draft.title }} · {{ ops.channels[item.provider] ?? item.provider }}
          </h3>
          <p class="mt-2 text-xs text-warning">{{ item.status }} · {{ item.message }}</p>
          <p v-if="item.status === 'unknown'" class="mt-3 text-xs leading-6">
            Kanal tarixini tekshiring. Qayta yuborish takroriy post yaratishi mumkin; bu ekran avtomatik qaytarmaydi.
          </p>
          <DeliveryPanel
            v-if="desk.workspace.value?.schedules.find((s) => s.id === item.scheduleId)"
            :plan="desk.workspace.value!.schedules.find((s) => s.id === item.scheduleId)!"
          />
          <Button variant="ghost" size="sm" class="mt-3" @click="desk.navigate('/calendar')">Taqvimda ko‘rish</Button>
        </article>
      </section>
      <section class="rounded-2xl border border-border bg-card p-5">
        <h2 class="font-editorial text-2xl">Vaqti o‘tgan rejalar</h2>
        <p class="mt-2 text-xs leading-6 text-muted-foreground">
          Bular yuborishga ruxsat berilmagan rejalar. Qo‘lda joylangan bo‘lsa, Natijalar bo‘limida qayd eting.
        </p>
        <p v-if="!overview.attentionPlans.length" class="mt-4 text-sm text-muted-foreground">Kechikkan reja yo‘q.</p>
        <article
          v-for="item in overview.attentionPlans"
          :key="item.id"
          class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl bg-muted p-4"
        >
          <div>
            <p class="text-sm font-semibold">{{ item.draftTitle }}</p>
            <p class="mt-1 text-xs">{{ ops.channels[item.channel] }} · {{ date(item.scheduledAt) }}</p>
          </div>
          <Button variant="outline" size="sm" @click="desk.navigate('/calendar')">Rejani ko‘rish</Button>
        </article>
      </section>
    </template>
  </div>
</template>
