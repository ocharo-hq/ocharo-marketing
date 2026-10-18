<script setup lang="ts">
import { useErrorToast } from '../../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { SkeletonState } from '@wow-two-beta/fe/presentation/feedback';
import { useMarketingWorkspace } from '../../application/marketing';
import { type OperationsOverviewModel } from '../../domain/marketing';
import { OperationsExtensions as ops } from '../../domain/marketing/OperationsExtensions';
import DeliveryPanel from './DeliveryPanel.vue';
const { t: ui, formatDate } = useUiLocale();

const desk = useMarketingWorkspace();
const overview = ref<OperationsOverviewModel | null>(null);
const refreshError = ref('');
const loading = ref(false);
let reading = false;
useErrorToast(refreshError);
let timer: ReturnType<typeof setTimeout> | undefined;
const abort = new AbortController();
let disposed = false;
function date(value: string | null) {
  return value ? formatDate(value, { hour: '2-digit', minute: '2-digit' }) : 'Kuzatilmagan';
}
async function load(quiet = false) {
  if (reading || disposed) return;
  if (timer) clearTimeout(timer);
  reading = true;
  loading.value = !quiet;
  const result = await desk.getOperations(abort.signal);
  if (disposed) return;
  if (result.ok) {
    overview.value = result.value;
    refreshError.value = '';
  } else if (result.failure.code !== 'cancelled') refreshError.value = desk.failureMessage(result.failure);
  loading.value = false;
  reading = false;
  // Read-only local state. No provider refresh, retry or delivery can result from this timer.
  if (!disposed) timer = setTimeout(() => load(true), 30000);
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
onMounted(() => load());
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
        <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Monitoring') }}</p>
        <h1 class="mt-2 font-editorial text-3xl">{{ ui('Where did posts stop?') }}</h1>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground">
          {{
            ui(
              'Local status refreshes every 30 seconds. Unapproved plans are not sent; deliveries with unknown outcomes are not retried automatically.',
            )
          }}
        </p>
      </div>
      <Button variant="outline" :is-disabled="loading" :is-loading="loading" @click="load()">{{
        ui('Refresh')
      }}</Button>
    </header>
    <div v-if="loading && !overview" aria-busy="true" class="query-region space-y-6">
      <div class="grid grid-cols-2 gap-3 xl:grid-cols-6">
        <article v-for="stat in 6" :key="stat" class="rounded-xl border border-border bg-card p-4" aria-hidden="true">
          <SkeletonState class="h-4 w-3/4" /><SkeletonState class="mt-3 h-9 w-16" />
        </article>
      </div>
      <div class="grid gap-4 md:grid-cols-3">
        <article
          v-for="worker in 3"
          :key="worker"
          class="rounded-xl border border-border bg-card p-5 space-y-3"
          aria-hidden="true"
        >
          <SkeletonState class="h-5 w-2/3" /><SkeletonState class="h-5 w-1/2" /><SkeletonState
            class="h-4 w-3/4"
          /><SkeletonState class="h-4 w-3/4" />
        </article>
      </div>
      <div class="rounded-xl border border-border bg-card p-5 space-y-4" aria-hidden="true">
        <SkeletonState class="h-8 w-64" /><SkeletonState class="h-5 w-3/4" /><SkeletonState class="h-10 w-full" />
      </div>
    </div>
    <p v-if="!loading && !overview" class="query-label" role="status">
      {{ ui('Content unavailable. Try refreshing.') }}
    </p>
    <div v-if="overview" :aria-busy="loading" class="space-y-6">
      <p class="text-xs text-muted-foreground">
        {{ ui('Saved state:') }} {{ date(overview.observedAt) }} · {{ overview.timeZone }}
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
          <p class="text-xs text-muted-foreground">{{ ui(stat[0]) }}</p>
          <SkeletonState v-if="loading" class="mt-3 h-9 w-16" />
          <p v-else class="mt-3 font-editorial text-3xl">{{ stat[1] }}</p>
        </article>
      </section>
      <section class="grid gap-4 md:grid-cols-3">
        <article
          v-for="worker in overview.workers"
          :key="worker.worker"
          class="rounded-xl border border-border bg-card p-5"
        >
          <h2 class="font-semibold">{{ ui(workerNames[worker.worker]) }}</h2>
          <p
            class="mt-3 text-sm"
            :class="['failed', 'stale', 'unobserved'].includes(worker.status) ? 'text-warning' : 'text-primary'"
          >
            {{ ui(workerStatus[worker.status] ?? worker.status) }}
          </p>
          <p class="mt-3 text-xs text-muted-foreground">{{ ui('Signal:') }} {{ date(worker.checkedAt) }}</p>
          <p class="mt-1 text-xs text-muted-foreground">
            {{ ui('Successful check:') }} {{ date(worker.lastSuccessAt) }}
          </p>
          <p v-if="worker.lastError" class="mt-3 text-xs text-warning">{{ ui('Needs attention') }}</p>
        </article>
      </section>
      <section class="rounded-xl border border-border bg-card p-5">
        <h2 class="font-editorial text-2xl">{{ ui('Delivery permissions') }}</h2>
        <p class="mt-3 text-sm">
          {{ ui('Live delivery:') }} {{ overview.delivery.publishingEnabled ? ui('Enabled') : ui('off') }}
          {{ ui('· Read channel metrics:') }} {{ overview.delivery.metricReadsEnabled ? ui('Enabled') : ui('off') }}
        </p>
        <p class="mt-3 text-xs leading-6 text-muted-foreground">
          {{
            ui(
              'A running queue service does not mean a channel is connected. Each delivery needs a reviewed post, destination and separate authorization.',
            )
          }}
        </p>
        <p class="mt-3 text-xs">
          {{ ui('Catalogue attempt:') }} {{ date(overview.automation.lastSyncAt) }} {{ ui('· Scheduling attempt:') }}
          {{ date(overview.automation.lastFillAt) }}
        </p>
        <p v-if="overview.automation.lastError" class="mt-3 text-xs text-warning">
          {{ ui('Last saved error (') }}{{ date(overview.automation.lastErrorAt) }}):
          {{ ui('Needs attention') }}
        </p>
        <Button class="mt-4" variant="outline" size="sm" @click="desk.navigate('/connections')">{{
          ui('Check connections')
        }}</Button>
      </section>
      <section class="rounded-2xl border border-border bg-card p-5">
        <h2 class="font-editorial text-2xl">{{ ui('Deliveries needing attention') }}</h2>
        <p v-if="!overview.attentionDeliveries.length" class="mt-4 text-sm text-muted-foreground">
          {{ ui('No failed, unknown or overdue authorized deliveries.') }}
        </p>
        <article
          v-for="item in overview.attentionDeliveries"
          :key="item.id"
          class="mt-4 rounded-xl border border-border p-4"
        >
          <h3 class="text-sm font-semibold">
            {{ item.snapshot.draft.title }} · {{ ops.channels[item.provider] ?? item.provider }}
          </h3>
          <p class="mt-2 text-xs text-warning">{{ ui(item.status) }} · {{ ui('Needs attention') }}</p>
          <p v-if="item.status === 'unknown'" class="mt-3 text-xs leading-6">
            {{
              ui(
                'Check the channel history. Sending again may duplicate the post; this screen does not retry automatically.',
              )
            }}
          </p>
          <DeliveryPanel
            v-if="desk.workspace.value?.schedules.find((s) => s.id === item.scheduleId)"
            :plan="desk.workspace.value!.schedules.find((s) => s.id === item.scheduleId)!"
          />
          <Button variant="ghost" size="sm" class="mt-3" @click="desk.navigate('/calendar')">{{
            ui('View in calendar')
          }}</Button>
        </article>
      </section>
      <section class="rounded-2xl border border-border bg-card p-5">
        <h2 class="font-editorial text-2xl">{{ ui('Overdue plans') }}</h2>
        <p class="mt-2 text-xs leading-6 text-muted-foreground">
          {{ ui('These plans are not authorized for delivery. Record manually published posts in Results.') }}
        </p>
        <p v-if="!overview.attentionPlans.length" class="mt-4 text-sm text-muted-foreground">
          {{ ui('No overdue plans.') }}
        </p>
        <article
          v-for="item in overview.attentionPlans"
          :key="item.id"
          class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl bg-muted p-4"
        >
          <div>
            <p class="text-sm font-semibold">{{ item.draftTitle }}</p>
            <p class="mt-1 text-xs">{{ ops.channels[item.channel] }} · {{ date(item.scheduledAt) }}</p>
          </div>
          <Button variant="outline" size="sm" @click="desk.navigate('/calendar')">{{ ui('View plan') }}</Button>
        </article>
      </section>
    </div>
  </div>
</template>
