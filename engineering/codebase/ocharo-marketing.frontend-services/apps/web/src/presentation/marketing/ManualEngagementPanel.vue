<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import {
  MarketingExtensions,
  type EngagementInput,
  type OperationsOverviewModel,
  type EngagementObservationModel,
} from '../../domain/marketing';
import { OperationsExtensions as ops } from '../../domain/marketing/OperationsExtensions';
import OperationsUnsavedGuard from './OperationsUnsavedGuard.vue';
const desk = useMarketingWorkspace();
const overview = ref<OperationsOverviewModel | null>(null);
const channel = ref('all');
const period = ref('30');
const saving = ref(false);
const open = ref(false);
const error = ref('');
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
  return MarketingExtensions.formatDate(value, { hour: '2-digit', minute: '2-digit' });
}
function delta(item: EngagementObservationModel, key: (typeof ops.metrics)[number]['key']) {
  const previous = ops.previous(item, overview.value?.observations ?? []);
  const old = previous?.observation[key];
  const current = item.observation[key];
  if (old === null || old === undefined || current === null) return null;
  const difference = current - old;
  return `${difference >= 0 ? '+' : ''}${difference}`;
}
async function load() {
  const result = await desk.getOperations(abort.signal);
  if (disposed) return;
  if (result.ok) overview.value = result.value;
  else if (result.failure.code !== 'cancelled') error.value = desk.failureMessage(result.failure);
}
function newObservation(item?: EngagementObservationModel) {
  if (dirty.value || saving.value) return;
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
  if (saving.value) return;
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
      await load();
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
  <section
    class="space-y-5 rounded-2xl border border-border bg-card p-5 sm:p-6"
    aria-labelledby="manual-engagement-heading"
  >
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h2 id="manual-engagement-heading" class="font-editorial text-2xl">Qo‘lda kuzatilgan natijalar</h2>
        <p class="mt-2 max-w-3xl text-xs leading-6 text-muted-foreground">
          Instagram, Telegram yoki saytdagi post natijalarini sanasi va manbasi bilan kiriting. Bular operator
          yozuvlari; kanal API tasdig‘i emas.
        </p>
      </div>
      <Button :is-disabled="dirty || saving" @click="newObservation()">Kuzatuv qo‘shish</Button>
    </div>
    <p v-if="error" role="alert" class="rounded-xl bg-warning-soft p-4 text-sm">{{ error }}</p>
    <form v-if="open" class="space-y-4 rounded-xl border border-border bg-background p-4" @submit.prevent="save">
      <h3 class="font-semibold">Postning jamlangan ko‘rsatkichlari</h3>
      <fieldset :disabled="saving" class="space-y-4">
        <div class="grid gap-4 sm:grid-cols-2">
          <label class="block text-xs font-semibold"
            >Kanal<select
              v-model="form.channel"
              class="mt-1 w-full rounded-lg border border-border bg-card p-2"
              @change="form.scheduleId = ''"
            >
              <option v-for="(label, key) in ops.channels" :key="key" :value="key">{{ label }}</option>
            </select></label
          >
          <label class="block text-xs font-semibold"
            >Mavjud reja (ixtiyoriy)<select
              v-model="form.scheduleId"
              class="mt-1 w-full rounded-lg border border-border bg-card p-2"
            >
              <option value="">Rejaga bog‘lanmagan</option>
              <option v-for="plan in eligibleSchedules" :key="plan.id" :value="plan.id">
                {{ plan.draftTitle || plan.id }}
              </option>
            </select></label
          >
        </div>
        <label class="block text-xs font-semibold"
          >Post nomi<input
            v-model="form.title"
            required
            maxlength="160"
            class="mt-1 w-full rounded-lg border border-border bg-card p-2"
        /></label>
        <label class="block text-xs font-semibold"
          >Joylangan post havolasi<input
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
            >Joylangan vaqt · Toshkent<input
              v-model="form.publishedAt"
              type="datetime-local"
              required
              class="mt-1 w-full min-w-0 rounded-lg border border-border bg-card p-2"
          /></label>
          <label class="block text-xs font-semibold"
            >Kuzatilgan vaqt · Toshkent<input
              v-model="form.observedAt"
              type="datetime-local"
              required
              class="mt-1 w-full min-w-0 rounded-lg border border-border bg-card p-2"
          /></label>
        </div>
        <p class="text-xs leading-6 text-muted-foreground">
          Ma’lumot yo‘q bo‘lsa bo‘sh qoldiring. Haqiqiy nol bo‘lsa 0 kiriting. So‘rov va buyurtmalar faqat siz
          tekshirgan bog‘lanishdir.
        </p>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <label v-for="metric in ops.metrics" :key="metric.key" class="block text-xs font-semibold"
            >{{ metric.label
            }}<input
              v-model="form[metric.key]"
              type="number"
              min="0"
              max="1000000000"
              step="1"
              placeholder="Noma’lum"
              class="mt-1 w-full rounded-lg border border-border bg-card p-2"
          /></label>
        </div>
        <label class="block text-xs font-semibold"
          >Manba / dalil izohi<textarea
            v-model="form.evidenceNote"
            rows="2"
            required
            maxlength="1000"
            placeholder="Masalan: Instagram Insights, shu postning qamrovi; 24 soatdan so‘ng o‘qildi."
            class="mt-1 w-full rounded-lg border border-border bg-card p-2"
          />
        </label>
      </fieldset>
      <div class="flex flex-wrap gap-3">
        <Button type="submit" :is-loading="saving" :is-disabled="saving">Kuzatuvni saqlash</Button
        ><Button type="button" variant="ghost" :is-disabled="saving" @click="close">Tahrirni bekor qilish</Button>
      </div>
      <p class="text-xs leading-5 text-muted-foreground">
        Yozuv o‘zgarmas tarixga qo‘shiladi. Xatoni tuzatish uchun keyin yangi kuzatuv qo‘shing.
      </p>
    </form>
    <div class="flex flex-wrap gap-3">
      <label class="text-xs font-semibold"
        >Kanal<select v-model="channel" class="ml-2 rounded-lg border border-border bg-background p-2">
          <option value="all">Barchasi</option>
          <option v-for="(label, key) in ops.channels" :key="key" :value="key">{{ label }}</option>
        </select></label
      >
      <label class="text-xs font-semibold"
        >Kuzatuv davri<select v-model="period" class="ml-2 rounded-lg border border-border bg-background p-2">
          <option value="7">7 kun</option>
          <option value="30">30 kun</option>
          <option value="90">90 kun</option>
          <option value="all">Barcha saqlangan</option>
        </select></label
      >
    </div>
    <p class="text-xs leading-6 text-muted-foreground">
      Har postning oxirgi kuzatuvi ko‘rsatiladi; takroriy jamlangan sonlar qo‘shilmaydi. Faollik = (yoqtirish + izoh +
      ulashish + saqlash) ÷ qamrov × 100. Biror son noma’lum bo‘lsa foiz hisoblanmaydi.
    </p>
    <p v-if="overview && overview.observationCount > overview.observations.length" class="text-xs text-warning">
      {{ overview.observationCount }} yozuvdan eng so‘nggi {{ overview.observations.length }} tasi ko‘rsatilgan.
      Haftalik hisob to‘liq tarixdan olinadi.
    </p>
    <p v-if="!latest.length" class="rounded-xl bg-muted p-5 text-sm text-muted-foreground">
      Bu davr uchun qo‘lda kuzatuv yo‘q. Natija nol deb hisoblanmaydi.
    </p>
    <div class="grid gap-4 xl:grid-cols-2">
      <article v-for="item in latest" :key="item.observation.id" class="min-w-0 rounded-xl border border-border p-4">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p class="text-[11px] text-muted-foreground">{{ ops.channels[item.observation.channel] }} · Qo‘lda</p>
            <h3 class="mt-1 font-semibold">{{ item.observation.title }}</h3>
          </div>
          <strong class="text-xl">{{ item.engagementRate === null ? '—' : `${item.engagementRate}%` }}</strong>
        </div>
        <a
          :href="item.observation.postUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="mt-2 block break-all text-xs text-primary"
          >Postni ochish</a
        >
        <p class="mt-3 text-xs text-muted-foreground">
          Kuzatildi: {{ date(item.observation.observedAt) }} · Joylandi: {{ date(item.observation.publishedAt) }}
        </p>
        <div class="mt-4 grid grid-cols-3 gap-3">
          <div v-for="metric in ops.metrics" :key="metric.key">
            <p class="text-[10px] text-muted-foreground">{{ metric.label }}</p>
            <p class="mt-1 text-sm font-semibold">
              {{ item.observation[metric.key] ?? '—' }}
              <span v-if="delta(item, metric.key) !== null" class="font-normal text-[10px] text-muted-foreground"
                >({{ delta(item, metric.key) }})</span
              >
            </p>
          </div>
        </div>
        <p class="mt-3 text-xs leading-6">{{ item.observation.evidenceNote }}</p>
        <p v-if="ops.previous(item, overview?.observations ?? [])" class="mt-2 text-[10px] text-muted-foreground">
          Qavsdagi farq: shu havolaning oldingi qo‘lda kuzatuviga nisbatan. Kanal API sonlari qo‘shilmaydi.
        </p>
        <Button size="sm" variant="outline" class="mt-4" :is-disabled="dirty || saving" @click="newObservation(item)"
          >Yangi o‘lchov qo‘shish</Button
        >
        <details class="mt-3 text-xs">
          <summary class="cursor-pointer text-primary">Shu postning kuzatuv tarixi</summary>
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
              {{ date(entry.observation.observedAt) }} · Qamrov {{ entry.observation.reach ?? '—' }} ·
              {{ entry.engagementRate === null ? 'Faollik noma’lum' : `${entry.engagementRate}%` }}
            </p>
            <p class="mt-1 text-muted-foreground">{{ entry.observation.evidenceNote }}</p>
          </div>
        </details>
      </article>
    </div>
    <OperationsUnsavedGuard :dirty="dirty || saving" />
  </section>
</template>
