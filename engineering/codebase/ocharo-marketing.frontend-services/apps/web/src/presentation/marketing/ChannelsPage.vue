<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import type { ChannelStrategyModel, OperationsOverviewModel } from '../../domain/marketing';
import { OperationsExtensions as ops } from '../../domain/marketing/OperationsExtensions';
import OperationsUnsavedGuard from './OperationsUnsavedGuard.vue';
const { t: ui, formatDate } = useUiLocale();

const desk = useMarketingWorkspace();
const overview = ref<OperationsOverviewModel | null>(null);
const editing = ref<ChannelStrategyModel | null>(null);
const baseline = ref('');
const saving = ref(false);
const loading = ref(false);
const dirty = computed(() => editing.value !== null && JSON.stringify(editing.value) !== baseline.value);
const abort = new AbortController();
let disposed = false;
async function load() {
  loading.value = true;
  const result = await desk.getOperations(abort.signal);
  if (disposed) return;
  if (result.ok) overview.value = result.value;
  else if (result.failure.code !== 'cancelled') desk.error.value = desk.failureMessage(result.failure);
  loading.value = false;
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
      await load();
      const current = overview.value?.channels.find((item) => item.strategy.channel === snapshot.channel)?.strategy;
      if (current && current.revision > snapshot.revision) {
        editing.value = { ...current };
        baseline.value = JSON.stringify(current);
      }
      desk.notice.value = 'Kanal rejasi saqlandi. Bu yuborishni yoqmaydi.';
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
        <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Channel map') }}</p>
        <h1 class="mt-2 font-editorial text-3xl">{{ ui('Each channel’s purpose') }}</h1>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground">
          {{
            ui(
              'Set the audience, topics, owner and weekly target. Plans, actual deliveries and manual posts are counted separately.',
            )
          }}
        </p>
      </div>
      <Button variant="outline" @click="desk.navigate('/automation')">{{ ui('Publishing schedule') }}</Button>
    </header>
    <p v-if="overview" class="text-xs text-muted-foreground">
      {{ ui('Week:') }} {{ formatDate(overview.weekStartsAt) }} – {{ formatDate(overview.weekEndsAt) }} ·
      {{ overview.timeZone }} {{ ui('· End date is excluded.') }}
    </p>
    <p class="text-xs leading-6 text-muted-foreground">
      {{
        ui(
          'Confirmed this week counts when channel confirmation arrived; older posts may be confirmed later. Manual posts use their publication date. An unlinked manual record may describe the same post; the two columns are not added together.',
        )
      }}
    </p>
    <div class="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(300px,420px)]">
      <div v-if="loading" aria-busy="true" class="grid gap-4 sm:grid-cols-2">
        <div v-for="card in 4" :key="card" class="h-64 animate-pulse rounded-2xl bg-muted" />
      </div>
      <div v-show="!loading" class="grid gap-4 sm:grid-cols-2">
        <article
          v-for="item in overview?.channels"
          :key="item.strategy.channel"
          class="min-w-0 rounded-2xl border border-border bg-card p-5"
        >
          <div class="flex flex-wrap items-center justify-between gap-3">
            <h2 class="font-editorial text-2xl">{{ ops.channels[item.strategy.channel] }}</h2>
            <span class="rounded-full bg-muted px-2 py-1 text-xs">{{
              item.strategy.active ? ui('Active in plan') : ui('No plan set')
            }}</span>
          </div>
          <p class="mt-3 text-sm">{{ item.strategy.purpose || ui('Set its purpose') }}</p>
          <dl class="mt-4 space-y-2 text-xs leading-5">
            <div>
              <dt class="text-muted-foreground">{{ ui('Audience') }}</dt>
              <dd>{{ item.strategy.audience || '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground">{{ ui('Themes') }}</dt>
              <dd>{{ item.strategy.pillars || '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground">{{ ui('Formats · owner') }}</dt>
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
          <p class="mt-3 text-[11px] leading-5 text-muted-foreground">
            {{ ui(ops.deliveryMode(item.strategy.channel)) }}
          </p>
          <div class="mt-4 grid grid-cols-2 gap-2 text-xs">
            <div class="rounded-lg bg-muted p-3">
              {{ ui('Weekly target')
              }}<strong class="mt-1 block text-lg">{{ item.strategy.weeklyTarget || '—' }}</strong>
            </div>
            <div class="rounded-lg bg-muted p-3">
              {{ ui('Weekly slots') }}<strong class="mt-1 block text-lg">{{ item.weeklySlots }}</strong>
            </div>
            <div class="rounded-lg bg-muted p-3">
              {{ ui('Confirmed this week')
              }}<strong class="mt-1 block text-lg">{{ item.confirmedReceiptsThisWeek }}</strong>
            </div>
            <div class="rounded-lg bg-muted p-3">
              {{ ui('Manually recorded') }}<strong class="mt-1 block text-lg">{{ item.manualPostsThisWeek }}</strong>
            </div>
          </div>
          <p class="mt-3 text-xs text-muted-foreground">
            {{ ui('Next 7 days:') }} {{ item.upcomingPlans }} {{ ui('plans. Results:') }}
            {{ item.lastObservationAt ? formatDate(item.lastObservationAt) : ui('No observation') }}.
          </p>
          <ul v-if="item.warnings.length" class="mt-3 space-y-1 text-xs leading-5 text-warning">
            <li v-for="warning in item.warnings" :key="warning">{{ ui(warning) }}</li>
          </ul>
          <Button
            class="mt-4"
            size="sm"
            variant="outline"
            :is-disabled="dirty || saving"
            @click="edit(item.strategy)"
            >{{ ui('Edit plan') }}</Button
          >
        </article>
      </div>
      <form
        v-if="editing"
        class="order-first space-y-4 rounded-2xl border border-border bg-card p-5 xl:sticky xl:top-6 xl:order-last"
        @submit.prevent="save"
      >
        <h2 class="font-editorial text-2xl">{{ ops.channels[editing.channel] }} {{ ui('plan') }}</h2>
        <fieldset :disabled="saving" class="space-y-4">
          <label class="flex gap-2 text-sm"
            ><input v-model="editing.active" type="checkbox" />{{ ui('Mark channel active in the plan') }}</label
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
            >{{ ui(field[1])
            }}<textarea
              v-model="editing[field[0]]"
              rows="2"
              maxlength="500"
              class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
            />
          </label>
          <label class="block text-xs font-semibold"
            >{{ ui('Owner')
            }}<input
              v-model="editing.owner"
              maxlength="120"
              class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
          /></label>
          <label class="block text-xs font-semibold"
            >{{ ui('Channel or page URL')
            }}<input
              v-model="editing.profileUrl"
              type="url"
              pattern="https://.*"
              placeholder="https://"
              maxlength="500"
              class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
          /></label>
          <div class="grid grid-cols-2 gap-3">
            <label class="block text-xs font-semibold"
              >{{ ui('Posts per week')
              }}<input
                v-model.number="editing.weeklyTarget"
                type="number"
                min="0"
                max="100"
                required
                class="mt-1 w-full rounded-lg border border-border bg-background p-2 text-sm"
            /></label>
            <label class="block text-xs font-semibold"
              >{{ ui('Results check interval (days)')
              }}<input
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
          {{ ui('This target does not create a schedule. Set times in Automation.') }}
        </p>
        <div class="flex flex-wrap gap-3">
          <Button type="submit" :is-disabled="saving || !dirty" :is-loading="saving">{{ ui('Save plan') }}</Button
          ><Button type="button" variant="ghost" :is-disabled="saving" @click="discard">{{
            ui('Close without saving')
          }}</Button>
        </div>
        <p v-if="dirty" class="text-xs text-warning">{{ ui('Unsaved changes') }}</p>
      </form>
      <section
        v-else
        class="rounded-2xl border border-dashed border-border p-6 text-sm leading-7 text-muted-foreground"
      >
        {{ ui('Open editing from a channel card. The initial state does not connect a channel or send posts.') }}
      </section>
    </div>
    <OperationsUnsavedGuard :dirty="dirty || saving" />
  </div>
</template>
