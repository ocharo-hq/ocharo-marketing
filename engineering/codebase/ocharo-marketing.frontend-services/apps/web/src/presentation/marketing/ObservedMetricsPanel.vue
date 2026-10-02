<script setup lang="ts">
import { useErrorToast } from '../../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { RefreshCw } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import StatusIndicator from './StatusIndicator.vue';
import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type MetricCacheModel, type ScheduleModel } from '../../domain/marketing';
const { t: ui, formatNumber, formatDate } = useUiLocale();

/** Reads saved observations on mount; a provider read requires the refresh button. */
defineOptions({ name: 'ObservedMetricsPanel' });
const props = defineProps<{ plan: ScheduleModel }>();
const desk = useMarketingWorkspace();
const cache = ref<MetricCacheModel | null>(null);
const loading = ref(false);
const refreshing = ref(false);
const error = ref<string | null>(null);
useErrorToast(error);
let loadedPlan: string | null = null;
let controller: AbortController | null = null;
let requestVersion = 0;
const operation = computed(() =>
  desk.workspace.value?.publicationOperations?.find((item) => item.scheduleId === props.plan.id),
);
const connection = computed(() =>
  desk.workspace.value?.connections.find((item) => item.id === operation.value?.connectionId),
);
const canRefresh = computed(
  () =>
    desk.workspace.value?.deliverySettings?.metricReadsEnabled === true &&
    connection.value?.metricReadsEnabled === true &&
    connection.value.verificationStatus === 'verified' &&
    operation.value?.provider === 'x' &&
    connection.value.capabilities.includes('public-metrics') &&
    connection.value.accountId === operation.value.snapshot.identity.accountId &&
    connection.value.destinationId === operation.value.snapshot.identity.destinationId &&
    operation.value?.receipt !== null &&
    operation.value?.receipt !== undefined &&
    props.plan.status === 'published',
);
const observations = computed(() =>
  [...(cache.value?.observations ?? [])].sort((a, b) => (b.observedAt ?? '').localeCompare(a.observedAt ?? '')),
);
const history = computed(() =>
  [...(cache.value?.history ?? cache.value?.observations ?? [])].sort((a, b) =>
    (b.observedAt ?? '').localeCompare(a.observedAt ?? ''),
  ),
);
const latest = computed(() => {
  const seen = new Set<string>();
  return observations.value.filter((item) => {
    const key = JSON.stringify([item.provider, item.receiptId, item.name, item.scope, item.aggregation]);
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
});
/** Cached GET does not contact the social provider. */
async function load(id: string) {
  controller?.abort();
  controller = new AbortController();
  const version = ++requestVersion;
  if (loadedPlan !== id) { cache.value = null; loadedPlan = id; }
  error.value = null;
  loading.value = true;
  const result = await desk.getMetrics(id, controller.signal);
  if (version !== requestVersion) return;
  if (result.ok) cache.value = result.value;
  else if (result.failure.code !== 'cancelled') error.value = desk.failureMessage(result.failure);
  loading.value = false;
}
watch(() => props.plan.id, load, { immediate: true });
onBeforeUnmount(() => {
  requestVersion++;
  controller?.abort();
});
/** A failed read keeps previous observations and reports the new attempt separately. */
async function refresh() {
  if (!canRefresh.value || refreshing.value) return;
  const id = props.plan.id;
  const version = requestVersion;
  refreshing.value = true;
  error.value = null;
  try {
    const result = await desk.refreshMetrics(id);
    if (version !== requestVersion || id !== props.plan.id) return;
    if (result.ok) await load(id);
    else error.value = desk.failureMessage(result.failure);
  } finally {
    refreshing.value = false;
  }
}
/** Provider metric names remain explicit while common names are readable. */
function name(value: string) {
  return (
    (
      {
        views: 'Ko‘rishlar',
        impressions: 'Ko‘rishlar',
        likes: 'Yoqtirishlar',
        like_count: 'Yoqtirishlar',
        replies: 'Javoblar',
        reply_count: 'Javoblar',
        reposts: 'Ulashishlar',
        retweet_count: 'Ulashishlar',
        quote_count: 'Iqtiboslar',
        bookmark_count: 'Saqlashlar',
        impression_count: 'Ko‘rishlar',
      } as Record<string, string>
    )[value] ?? value
  );
}
</script>
<template>
  <article class="rounded-2xl border border-border bg-card p-5 sm:p-6">
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h2 class="font-editorial text-2xl">
          {{ plan.draftTitle ?? operation?.snapshot.draft.title ?? ui('Published post') }}
        </h2>
        <p class="mt-2 text-xs text-muted-foreground">
          {{ plan.channel }} · {{ plan.draftRevision }}{{ ui(' revision ·') }}
          {{ formatDate(plan.scheduledAt, { hour: '2-digit', minute: '2-digit' }) }}
        </p>
      </div>
      <StatusIndicator :status="plan.status" />
    </div>
    <p v-if="operation?.receipt" class="mt-3 break-all text-[11px] text-muted-foreground">
      {{ ui('Confirmation:') }} {{ operation.receipt.externalId }} · {{ ui(operation.receipt.stage) }}
    </p>
    <div class="mt-5 flex flex-wrap items-center gap-3">
      <Button
        variant="outline"
        size="sm"
        :is-disabled="!canRefresh || refreshing || desk.busy.value !== null"
        :is-loading="refreshing"
        @click="refresh"
        ><Icon :icon="RefreshCw" :size="14" class="mr-2" />{{ ui('Refresh from channel') }}</Button
      ><span class="text-[11px] leading-5 text-muted-foreground">{{
        plan.channel === 'x'
          ? ui('Reading X data may use credits. Data is read only when the button is pressed.')
          : ui('The channel is read only when this button is pressed.')
      }}</span>
    </div>
    <p v-if="!canRefresh" class="mt-3 text-xs leading-6 text-muted-foreground">
      {{
        ui(
          'Requires a confirmed publication, a verified connection and permission to read metrics. Only saved observations appear below.',
        )
      }}
    </p>
    <div v-if="loading" aria-busy="true" class="mt-5 grid gap-3 sm:grid-cols-2"><div v-for="card in 4" :key="card" class="h-28 animate-pulse rounded-xl bg-muted" /></div>
    <div v-if="cache?.lastAttempt" class="mt-4 rounded-lg bg-muted p-3 text-xs leading-6">
      <p>
        {{ ui('Last request:') }}
        {{ formatDate(cache.lastAttempt.attemptedAt, { hour: '2-digit', minute: '2-digit' }) }} ·
        {{ ui(MarketingExtensions.statusLabel(cache.lastAttempt.status)) }}
      </p>
      <p class="text-muted-foreground">{{ ui('Needs attention') }}</p>
    </div>
    <div v-if="latest.length" v-show="!loading" class="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <div v-for="item in latest" :key="item.id" class="rounded-xl border border-border p-4">
        <p class="text-xs font-semibold">{{ ui(name(item.name)) }}</p>
        <p class="mt-3 font-editorial text-3xl">
          {{ item.value === null ? '—' : formatNumber(item.value) }}
        </p>
        <p class="mt-3 text-[11px] leading-5 text-muted-foreground">
          {{ item.scope === 'post' ? ui('Post') : ui('Account') }} ·
          {{ item.aggregation === 'cumulative' ? ui('Cumulative') : item.aggregation }} ·
          {{ ui(MarketingExtensions.statusLabel(item.availability)) }}
        </p>
        <p v-if="item.observedAt" class="mt-2 text-[11px] text-muted-foreground">
          {{ ui('Observed:') }} {{ formatDate(item.observedAt, { hour: '2-digit', minute: '2-digit' }) }}
        </p>
        <p v-if="item.reason" class="mt-2 text-xs leading-6 text-muted-foreground">{{ ui(item.reason) }}</p>
      </div>
    </div>
    <p v-else-if="!loading" class="mt-5 text-sm leading-7 text-muted-foreground">
      {{ ui('No saved observations. This does not mean zero results.') }}
    </p>
    <details v-if="history.length" class="mt-5 text-xs">
      <summary class="cursor-pointer font-semibold text-primary">
        {{ cache?.history ? ui('Observation history') : ui('Saved observations') }} · {{ history.length }}
      </summary>
      <p v-if="cache?.historyTruncated" class="mt-3 text-[11px] leading-6 text-muted-foreground">
        {{ ui('Showing the latest 250 observations; earlier history is preserved.') }}
      </p>
      <div class="mt-3 space-y-3">
        <div v-for="item in history" :key="item.id" class="rounded-lg bg-muted/40 p-3">
          <p>
            {{ ui(name(item.name)) }}: {{ item.value === null ? ui('No data') : item.value }} · {{ ui(item.scope) }} ·
            {{ ui(item.aggregation) }}
          </p>
          <p class="mt-1 break-all text-[11px] leading-6 text-muted-foreground">
            {{ item.provider }} {{ ui('· Confirmation') }} {{ item.receiptId }} ·
            {{
              item.observedAt
                ? formatDate(item.observedAt, { hour: '2-digit', minute: '2-digit' })
                : ui('No observation date')
            }}
          </p>
        </div>
      </div>
    </details>
  </article>
</template>
