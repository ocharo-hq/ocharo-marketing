<script setup lang="ts">
import { requestFailureMessage } from '@ocharomarketing/connect/RequestFeedback';
import { SkeletonState } from '@wow-two-beta/ui-vue/presentation/feedback';
import { useErrorToast } from '../../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import {
  type EngagementInput,
  type OperationsOverviewModel,
  type EngagementObservationModel,
} from '../../domain/marketing';
import { OperationsExtensions as ops } from '../../domain/marketing/OperationsExtensions';
import OperationsUnsavedGuard from './OperationsUnsavedGuard.vue';
const { t: ui, formatDate } = useUiLocale();

const desk = useMarketingWorkspace();
const overview = ref<OperationsOverviewModel | null>(null);
const channel = ref('all');
const period = ref('30');
const saving = ref(false);
const loading = ref(false);
const open = ref(false);
const error = ref('');
useErrorToast(error);
const abort = new AbortController();
let disposed = false;
function localInput(date: Date) {
  return new Date(date.getTime() + 5 * 3600000).toISOString().slice(0, 16);
}
function fresh() {
  return {
    id: crypto.randomUUID(),
    channel: 'instagram',
    postUrl: '',
    title: '',
    scheduleId: '',
    publishedAt: localInput(new Date()),
    observedAt: localInput(new Date()),
    evidenceNote: '',
    views: '',
    reach: '',
    likes: '',
    comments: '',
    shares: '',
    saves: '',
    clicks: '',
    enquiries: '',
    orders: '',
  };
}
const form = reactive(fresh());
const baseline = ref(JSON.stringify(form));
const dirty = computed(() => JSON.stringify(form) !== baseline.value);
const filtered = computed(() =>
  (overview.value?.observations ?? []).filter(
    (item) =>
      (channel.value === 'all' || item.observation.channel === channel.value) &&
      (period.value === 'all' ||
        Date.parse(item.observation.observedAt) >= Date.now() - Number(period.value) * 86400000),
  ),
);
const latest = computed(() => ops.latest(filtered.value));
const eligibleSchedules = computed(() =>
  (desk.workspace.value?.schedules ?? []).filter((s) => s.channel === form.channel),
);
function date(value: string) {
  return formatDate(value, { hour: '2-digit', minute: '2-digit' });
}
function delta(item: EngagementObservationModel, key: (typeof ops.metrics)[number]['key']) {
  const previous = ops.previous(item, overview.value?.observations ?? []);
  const old = previous?.observation[key];
  const current = item.observation[key];
  if (old === null || old === undefined || current === null) return null;
  const difference = current - old;
  return `${difference >= 0 ? '+' : ''}${difference}`;
}
async function load(afterSave = false) {
  if (loading.value || (!afterSave && (saving.value || dirty.value))) return;
  loading.value = true;
  try {
    const result = await desk.getOperations(abort.signal);
    if (disposed) return;
    if (result.ok) {
      overview.value = result.value;
      error.value = '';
    } else if (result.failure.code !== 'cancelled') error.value = desk.failureMessage(result.failure);
  } catch {
    if (!disposed) error.value = requestFailureMessage({ code: 'transport', status: 0 });
  } finally {
    if (!disposed) loading.value = false;
  }
}
function newObservation(item?: EngagementObservationModel) {
  if (dirty.value || saving.value || loading.value) return;
  Object.assign(form, fresh());
  if (item)
    Object.assign(form, {
      channel: item.observation.channel,
      postUrl: item.observation.postUrl,
      title: item.observation.title,
      scheduleId: item.observation.scheduleId ?? '',
      publishedAt: localInput(new Date(item.observation.publishedAt)),
    });
  baseline.value = JSON.stringify(form);
  open.value = true;
  error.value = '';
}
function close() {
  if (saving.value) return;
  Object.assign(form, fresh());
  baseline.value = JSON.stringify(form);
  open.value = false;
  error.value = '';
}
async function save() {
  if (saving.value || loading.value) return;
  const observed = new Date(`${form.observedAt}:00+05:00`);
  const published = new Date(`${form.publishedAt}:00+05:00`);
  if (!Number.isFinite(observed.getTime()) || !Number.isFinite(published.getTime())) {
    error.value = 'Sana va vaqtni to‘ldiring.';
    return;
  }
  const values = Object.fromEntries(
    ops.metrics.map(({ key }) => [key, form[key] === '' ? null : Number(form[key])]),
  ) as Pick<EngagementInput, (typeof ops.metrics)[number]['key']>;
  const snapshot: EngagementInput = {
    ...values,
    id: form.id,
    channel: form.channel,
    title: form.title.trim(),
    postUrl: form.postUrl.trim(),
    scheduleId: form.scheduleId || null,
    publishedAt: published.toISOString(),
    observedAt: observed.toISOString(),
    evidenceNote: form.evidenceNote.trim(),
  };
  saving.value = true;
  error.value = '';
  try {
    const result = await desk.recordEngagement(snapshot);
    if (disposed) return;
    if (result.ok) {
      Object.assign(form, fresh());
      baseline.value = JSON.stringify(form);
      open.value = false;
      desk.notice.value = 'Qo‘lda kuzatuv saqlandi. Kanal tasdig‘i o‘zgarmadi.';
      await load(true);
    } else error.value = desk.failureMessage(result.failure);
  } finally {
    saving.value = false;
  }
}
onMounted(load);
onBeforeUnmount(() => {
  disposed = true;
  abort.abort();
});
</script>
<template>
  <section class="space-y-5" aria-labelledby="manual-engagement-heading">
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h2 id="manual-engagement-heading" class="font-editorial text-2xl">{{ ui('Manually observed results') }}</h2>
        <p class="mt-2 max-w-3xl text-xs leading-6 text-muted-foreground">
          {{
            ui(
              'Enter Instagram, Telegram or website post results with the date and source. These are operator records, not channel API confirmations.',
            )
          }}
        </p>
      </div>
      <div class="flex flex-wrap gap-3">
        <Button variant="outline" :is-disabled="dirty || saving || loading" :is-loading="loading" @click="load()">{{
          ui('Refresh')
        }}</Button>
        <Button :is-disabled="dirty || saving || loading" @click="newObservation()">{{ ui('Add observation') }}</Button>
      </div>
    </div>

    <form v-if="open" class="space-y-4 rounded-xl border border-border bg-background p-4" @submit.prevent="save">
      <h3 class="font-semibold">{{ ui('Cumulative post metrics') }}</h3>
      <fieldset :disabled="saving" class="space-y-4">
        <div class="grid gap-4 sm:grid-cols-2">
          <label class="block text-xs font-semibold"
            >{{ ui('Channel')
            }}<select
              v-model="form.channel"
              class="mt-1 w-full rounded-lg border border-border bg-card p-2"
              @change="form.scheduleId = ''"
            >
              <option v-for="(label, key) in ops.channels" :key="key" :value="key">{{ ui(label) }}</option>
            </select></label
          >
          <label class="block text-xs font-semibold"
            >{{ ui('Existing plan (optional)')
            }}<select v-model="form.scheduleId" class="mt-1 w-full rounded-lg border border-border bg-card p-2">
              <option value="">{{ ui('Not linked to a plan') }}</option>
              <option v-for="plan in eligibleSchedules" :key="plan.id" :value="plan.id">
                {{ plan.draftTitle || plan.id }}
              </option>
            </select></label
          >
        </div>
        <label class="block text-xs font-semibold"
          >{{ ui('Post name')
          }}<input
            v-model="form.title"
            required
            maxlength="160"
            class="mt-1 w-full rounded-lg border border-border bg-card p-2"
        /></label>
        <label class="block text-xs font-semibold"
          >{{ ui('Published post URL')
          }}<input
            v-model="form.postUrl"
            type="url"
            pattern="https://.*"
            required
            maxlength="500"
            placeholder="https://"
            class="mt-1 w-full rounded-lg border border-border bg-card p-2"
        /></label>
        <div class="grid gap-4 sm:grid-cols-2">
          <label class="block text-xs font-semibold"
            >{{ ui('Published at · Tashkent')
            }}<input
              v-model="form.publishedAt"
              type="datetime-local"
              required
              class="mt-1 w-full min-w-0 rounded-lg border border-border bg-card p-2"
          /></label>
          <label class="block text-xs font-semibold"
            >{{ ui('Observed at · Tashkent')
            }}<input
              v-model="form.observedAt"
              type="datetime-local"
              required
              class="mt-1 w-full min-w-0 rounded-lg border border-border bg-card p-2"
          /></label>
        </div>
        <p class="text-xs leading-6 text-muted-foreground">
          {{
            ui(
              'Leave unknown values blank. Enter 0 only for a real zero. Enquiries and orders are linked only as verified by you.',
            )
          }}
        </p>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <label v-for="metric in ops.metrics" :key="metric.key" class="block text-xs font-semibold"
            >{{ ui(metric.label)
            }}<input
              v-model="form[metric.key]"
              type="number"
              min="0"
              max="1000000000"
              step="1"
              :placeholder="ui('Unknown')"
              class="mt-1 w-full rounded-lg border border-border bg-card p-2"
          /></label>
        </div>
        <label class="block text-xs font-semibold"
          >{{ ui('Source / evidence note')
          }}<textarea
            v-model="form.evidenceNote"
            rows="2"
            required
            maxlength="1000"
            :placeholder="ui('For example: Instagram Insights, reach for this post, read after 24 hours.')"
            class="mt-1 w-full rounded-lg border border-border bg-card p-2"
          />
        </label>
      </fieldset>
      <div class="flex flex-wrap gap-3">
        <Button type="submit" :is-loading="saving" :is-disabled="saving">{{ ui('Save observation') }}</Button
        ><Button type="button" variant="ghost" :is-disabled="saving" @click="close">{{ ui('Discard changes') }}</Button>
      </div>
      <p class="text-xs leading-5 text-muted-foreground">
        {{ ui('The record enters immutable history. To correct an error, add a new observation later.') }}
      </p>
    </form>
    <div class="flex flex-wrap gap-3">
      <label class="text-xs font-semibold"
        >{{ ui('Channel')
        }}<select v-model="channel" class="ml-2 rounded-lg border border-border bg-background p-2">
          <option value="all">{{ ui('All') }}</option>
          <option v-for="(label, key) in ops.channels" :key="key" :value="key">{{ ui(label) }}</option>
        </select></label
      >
      <label class="text-xs font-semibold"
        >{{ ui('Observation period')
        }}<select v-model="period" class="ml-2 rounded-lg border border-border bg-background p-2">
          <option value="7">{{ ui('7 days') }}</option>
          <option value="30">{{ ui('30 days') }}</option>
          <option value="90">{{ ui('90 days') }}</option>
          <option value="all">{{ ui('All saved') }}</option>
        </select></label
      >
    </div>
    <p class="text-xs leading-6 text-muted-foreground">
      {{
        ui(
          'Shows each post’s latest observation without adding repeated cumulative counts. Engagement = (likes + comments + shares + saves) ÷ reach × 100. No percentage is calculated when any value is unknown.',
        )
      }}
    </p>
    <p v-if="overview && overview.observationCount > overview.observations.length" class="text-xs text-warning">
      {{ overview.observationCount }} {{ ui('latest records out of') }} {{ overview.observations.length }}
      {{ ui('shown. Weekly totals use the full history.') }}
    </p>
    <div v-if="loading && !overview" class="query-region grid gap-4 xl:grid-cols-2" aria-busy="true">
      <article v-for="card in 2" :key="card" class="space-y-4 rounded-xl border border-border p-4" aria-hidden="true">
        <SkeletonState class="h-4 w-24" /><SkeletonState class="h-6 w-1/2" /><SkeletonState class="h-4 w-40" />
        <div class="grid grid-cols-3 gap-3">
          <div v-for="metric in ops.metrics" :key="metric.key" class="space-y-2">
            <SkeletonState class="h-3 w-16" /><SkeletonState class="h-5 w-12" />
          </div>
        </div>
        <SkeletonState class="h-5 w-full" /><SkeletonState class="h-9 w-40" />
      </article>
    </div>
    <p v-else-if="!overview" class="query-label">{{ ui('Manual observations unavailable') }}</p>
    <p v-else-if="!latest.length" class="query-label">
      {{ ui('No manual observations for this period. Missing data is not counted as zero.') }}
    </p>
    <div class="grid gap-4 xl:grid-cols-2" :aria-busy="loading">
      <article v-for="item in latest" :key="item.observation.id" class="min-w-0 rounded-xl border border-border p-4">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p class="text-[11px] text-muted-foreground">
              {{ ops.channels[item.observation.channel] }} {{ ui('· Manual') }}
            </p>
            <h3 class="mt-1 font-semibold">{{ item.observation.title }}</h3>
          </div>
          <SkeletonState v-if="loading" class="h-7 w-16" /><strong v-else class="text-xl">{{
            item.engagementRate === null ? '—' : `${item.engagementRate}%`
          }}</strong>
        </div>
        <a
          :href="item.observation.postUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="mt-2 block break-all text-xs text-primary"
          >{{ ui('Open post') }}</a
        >
        <p class="mt-3 text-xs text-muted-foreground">
          {{ ui('Observed:') }} {{ date(item.observation.observedAt) }} {{ ui('· Published:') }}
          {{ date(item.observation.publishedAt) }}
        </p>
        <div class="mt-4 grid grid-cols-3 gap-3">
          <div v-for="metric in ops.metrics" :key="metric.key">
            <p class="text-[10px] text-muted-foreground">{{ ui(metric.label) }}</p>
            <SkeletonState v-if="loading" class="mt-1 h-5 w-12" />
            <p v-else class="mt-1 text-sm font-semibold">
              {{ item.observation[metric.key] ?? '—' }}
              <span v-if="delta(item, metric.key) !== null" class="font-normal text-[10px] text-muted-foreground"
                >({{ delta(item, metric.key) }})</span
              >
            </p>
          </div>
        </div>
        <p class="mt-3 text-xs leading-6">{{ item.observation.evidenceNote }}</p>
        <p v-if="ops.previous(item, overview?.observations ?? [])" class="mt-2 text-[10px] text-muted-foreground">
          {{
            ui(
              'The difference in brackets compares the previous manual observation of the same URL. Channel API counts are not added.',
            )
          }}
        </p>
        <Button
          size="sm"
          variant="outline"
          class="mt-4"
          :is-disabled="dirty || saving || loading"
          @click="newObservation(item)"
          >{{ ui('Add a new measurement') }}</Button
        >
        <details class="mt-3 text-xs">
          <summary class="cursor-pointer text-primary">{{ ui('Observation history for this post') }}</summary>
          <div
            v-for="entry in overview?.observations.filter(
              (x) =>
                x.observation.channel === item.observation.channel &&
                x.observation.postUrl === item.observation.postUrl,
            )"
            :key="entry.observation.id"
            class="mt-3 rounded-lg bg-muted p-3"
          >
            <p>
              {{ date(entry.observation.observedAt) }} {{ ui('· Reach') }} {{ entry.observation.reach ?? '—' }} ·
              {{ entry.engagementRate === null ? ui('Engagement unknown') : `${entry.engagementRate}%` }}
            </p>
            <p class="mt-1 text-muted-foreground">{{ entry.observation.evidenceNote }}</p>
          </div>
        </details>
      </article>
    </div>
    <OperationsUnsavedGuard :dirty="dirty || saving" />
  </section>
</template>
