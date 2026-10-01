<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import type { ChannelStrategyModel, OperationsOverviewModel } from '../../domain/marketing';
import { MarketingExtensions } from '../../domain/marketing';
import { OperationsExtensions as ops } from '../../domain/marketing/OperationsExtensions';
import OperationsUnsavedGuard from './OperationsUnsavedGuard.vue';
const desk = useMarketingWorkspace();
const overview = ref<OperationsOverviewModel | null>(null);
const editing = ref<ChannelStrategyModel | null>(null);
const baseline = ref('');
const saving = ref(false);
const dirty = computed(() => editing.value !== null && JSON.stringify(editing.value) !== baseline.value);
const abort = new AbortController();
let disposed = false;
async function load() {
  const result = await desk.getOperations(abort.signal);
  if (disposed) return;
  if (result.ok) overview.value = result.value;
  else if (result.failure.code !== 'cancelled') desk.error.value = desk.failureMessage(result.failure);
}
function edit(strategy: ChannelStrategyModel) {
  if (dirty.value || saving.value) return;
  editing.value = { ...strategy };
  baseline.value = JSON.stringify(editing.value);
}
function discard() {
  editing.value = null;
  baseline.value = '';
}
async function save() {
  if (!editing.value || saving.value) return;
  const snapshot = { ...editing.value };
  saving.value = true;
  try {
    const result = await desk.saveChannelStrategy(snapshot);
    if (disposed) return;
    if (result.ok) {
      editing.value = { ...result.value };
      baseline.value = JSON.stringify(result.value);
      desk.notice.value = 'Kanal rejasi saqlandi. Bu yuborishni yoqmaydi.';
      await load();
    }
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
  <div class="space-y-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">Kanallar xaritasi</p>
        <h1 class="mt-2 font-editorial text-3xl">Har bir kanalning vazifasi</h1>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground">
          Auditoriya, mavzular, egasi va haftalik maqsadni belgilang. Reja, haqiqiy yuborish va qo‘lda qayd etilgan
          postlar alohida hisoblanadi.
        </p>
      </div>
      <Button variant="outline" @click="desk.navigate('/automation')">Joylash jadvali</Button>
    </header>
    <p v-if="overview" class="text-xs text-muted-foreground">
      Hafta: {{ MarketingExtensions.formatDate(overview.weekStartsAt) }} –
      {{ MarketingExtensions.formatDate(overview.weekEndsAt) }} · {{ overview.timeZone }} · Yakun sanasi kirmaydi.
    </p>
    <p class="text-xs leading-6 text-muted-foreground">
      “Haftada tasdiqlandi” kanal tasdig‘i kelgan vaqtni bildiradi; eski post keyin tasdiqlanishi mumkin. Qo‘lda
      joylanganlar o‘z joylash sanasi bo‘yicha sanaladi. Rejaga bog‘lanmagan qo‘lda yozuv bir xil postni qayd etishi
      mumkin; ikki ustun qo‘shilmaydi.
    </p>
    <div class="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(300px,420px)]">
      <div class="grid gap-4 sm:grid-cols-2">
        <article
          v-for="item in overview?.channels"
          :key="item.strategy.channel"
          class="min-w-0 rounded-2xl border border-border bg-card p-5"
        >
          <div class="flex flex-wrap items-center justify-between gap-3">
            <h2 class="font-editorial text-2xl">{{ ops.channels[item.strategy.channel] }}</h2>
            <span class="rounded-full bg-muted px-2 py-1 text-xs">{{
              item.strategy.active ? 'Rejada faol' : 'Reja belgilanmagan'
            }}</span>
          </div>
          <p class="mt-3 text-sm">{{ item.strategy.purpose || 'Vazifasini belgilang' }}</p>
          <dl class="mt-4 space-y-2 text-xs leading-5">
            <div>
              <dt class="text-muted-foreground">Auditoriya</dt>
              <dd>{{ item.strategy.audience || '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground">Mavzular</dt>
              <dd>{{ item.strategy.pillars || '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground">Formatlar · mas’ul</dt>
              <dd>{{ item.strategy.formats || '—' }} · {{ item.strategy.owner || '—' }}</dd>
            </div>
          </dl>
          <a
            v-if="item.strategy.profileUrl"
            :href="item.strategy.profileUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="mt-3 block break-all text-xs text-primary"
            >{{ item.strategy.profileUrl }}</a
          >
          <p class="mt-3 text-[11px] leading-5 text-muted-foreground">{{ ops.deliveryMode(item.strategy.channel) }}</p>
          <div class="mt-4 grid grid-cols-2 gap-2 text-xs">
            <div class="rounded-lg bg-muted p-3">
              Haftalik maqsad<strong class="mt-1 block text-lg">{{ item.strategy.weeklyTarget || '—' }}</strong>
            </div>
            <div class="rounded-lg bg-muted p-3">
              Haftalik vaqtlar<strong class="mt-1 block text-lg">{{ item.weeklySlots }}</strong>
            </div>
            <div class="rounded-lg bg-muted p-3">
              Haftada tasdiqlandi<strong class="mt-1 block text-lg">{{ item.confirmedReceiptsThisWeek }}</strong>
            </div>
            <div class="rounded-lg bg-muted p-3">
              Qo‘lda qayd etilgan<strong class="mt-1 block text-lg">{{ item.manualPostsThisWeek }}</strong>
            </div>
          </div>
          <p class="mt-3 text-xs text-muted-foreground">
            Keyingi 7 kun: {{ item.upcomingPlans }} reja. Natijalar:
            {{ item.lastObservationAt ? MarketingExtensions.formatDate(item.lastObservationAt) : 'Kuzatuv yo‘q' }}.
          </p>
          <ul v-if="item.warnings.length" class="mt-3 space-y-1 text-xs leading-5 text-warning">
            <li v-for="warning in item.warnings" :key="warning">{{ warning }}</li>
          </ul>
          <Button class="mt-4" size="sm" variant="outline" :is-disabled="dirty || saving" @click="edit(item.strategy)"
            >Rejani tahrirlash</Button
          >
        </article>
      </div>
      <form
        v-if="editing"
        class="order-first space-y-4 rounded-2xl border border-border bg-card p-5 xl:sticky xl:top-24 xl:order-last"
        @submit.prevent="save"
      >
        <h2 class="font-editorial text-2xl">{{ ops.channels[editing.channel] }} rejasi</h2>
        <fieldset :disabled="saving" class="space-y-4">
          <label class="flex gap-2 text-sm"
            ><input v-model="editing.active" type="checkbox" />Kanalni rejada faol deb belgilash</label
          >
          <label
            v-for="field in [
              ['purpose', 'Vazifa'],
              ['audience', 'Auditoriya'],
              ['pillars', 'Mavzular / kontent yo‘nalishlari'],
              ['formats', 'Formatlar'],
            ] as const"
            :key="field[0]"
            class="block text-xs font-semibold"
            >{{ field[1]
            }}<textarea
              v-model="editing[field[0]]"
              rows="2"
              maxlength="500"
              class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
            />
          </label>
          <label class="block text-xs font-semibold"
            >Mas’ul<input
              v-model="editing.owner"
              maxlength="120"
              class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
          /></label>
          <label class="block text-xs font-semibold"
            >Kanal yoki sahifa havolasi<input
              v-model="editing.profileUrl"
              type="url"
              pattern="https://.*"
              placeholder="https://"
              maxlength="500"
              class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
          /></label>
          <div class="grid grid-cols-2 gap-3">
            <label class="block text-xs font-semibold"
              >Haftasiga postlar<input
                v-model.number="editing.weeklyTarget"
                type="number"
                min="0"
                max="100"
                required
                class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
            /></label>
            <label class="block text-xs font-semibold"
              >Natijalarni tekshirish (kun)<input
                v-model.number="editing.reviewEveryDays"
                type="number"
                min="1"
                max="90"
                required
                class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
            /></label>
          </div>
        </fieldset>
        <p class="text-xs leading-5 text-muted-foreground">
          Bu maqsad avtomatik jadval yaratmaydi. Vaqtlarni Avtomatika bo‘limida belgilang.
        </p>
        <div class="flex flex-wrap gap-3">
          <Button type="submit" :is-disabled="saving || !dirty" :is-loading="saving">Rejani saqlash</Button
          ><Button type="button" variant="ghost" :is-disabled="saving" @click="discard">Saqlamasdan yopish</Button>
        </div>
        <p v-if="dirty" class="text-xs text-warning">Saqlanmagan o‘zgarishlar</p>
      </form>
      <section
        v-else
        class="rounded-2xl border border-dashed border-border p-6 text-sm leading-7 text-muted-foreground"
      >
        Kanal kartasidan tahrirlashni oching. Dastlabki holat kanalga ulanmaydi va hech qanday post yubormaydi.
      </section>
    </div>
    <OperationsUnsavedGuard :dirty="dirty || saving" />
  </div>
</template>
