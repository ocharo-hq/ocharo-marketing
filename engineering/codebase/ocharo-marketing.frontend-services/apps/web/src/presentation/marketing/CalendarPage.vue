<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, ref } from 'vue';
import { CalendarDays, ChevronLeft, ChevronRight, Plus, X } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import { type ScheduleModel } from '../../domain/marketing';
import StatusIndicator from './StatusIndicator.vue';
import DeliveryPanel from './DeliveryPanel.vue';
const { t: ui, formatDate } = useUiLocale();

/** Displays recorded plans with their publishing state and exact approved revision. */
defineOptions({ name: 'CalendarPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const offset = ref(0);
const today = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Tashkent' }).format(new Date());
const channelNames: Record<string, string> = {
  instagram: 'Instagram',
  telegram: 'Telegram',
  whatsapp: 'WhatsApp',
  x: 'X',
};
/** The viewed week uses Tashkent calendar dates. */
const days = computed(() => {
  const first = new Date(`${today}T00:00:00Z`);
  first.setUTCDate(first.getUTCDate() - ((first.getUTCDay() + 6) % 7) + offset.value * 7);
  return Array.from({ length: 7 }, (_, index) => {
    const day = new Date(first);
    day.setUTCDate(day.getUTCDate() + index);
    const date = day.toISOString().slice(0, 10);
    return {
      date,
      label: ['Yak', 'Dush', 'Sesh', 'Chor', 'Pay', 'Jum', 'Shan'][day.getUTCDay()],
      number: day.getUTCDate(),
      schedules:
        workspace.value?.schedules.filter(
          (item) =>
            new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Tashkent' }).format(new Date(item.scheduledAt)) === date,
        ) ?? [],
    };
  });
});
/** The agenda exposes saved plans outside the visible week as well. */
const agenda = computed(() =>
  [...(workspace.value?.schedules ?? [])].sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt)),
);
/** Resolves the saved post title without fabricating a missing record. */
function title(plan: ScheduleModel) {
  const draft = workspace.value?.drafts.find((item) => item.id === plan.draftId);
  return plan.draftTitle ?? (draft?.revision === plan.draftRevision ? draft.title : 'Saqlangan post');
}
/** A newer draft cannot change the displayed scheduled snapshot. */
function changed(plan: ScheduleModel) {
  return workspace.value?.drafts.find((item) => item.id === plan.draftId)?.revision !== plan.draftRevision;
}
/** Cancels a pending plan without claiming already-published content was removed. */
async function cancel(id: string) {
  const result = await desk.cancelSchedule(id);
  if (result.ok) desk.notice.value = 'Reja bekor qilindi.';
}
</script>

<template>
  <div class="space-y-7">
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Post calendar') }}</p>
        <h1 class="mt-2 font-editorial text-3xl">{{ ui('Steady, without rushing.') }}</h1>
        <p class="mt-3 max-w-xl text-sm leading-7 text-muted-foreground">
          {{ ui('Times use Tashkent. Planned means the post has not been published yet.') }}
        </p>
      </div>
      <Button @click="desk.navigate('/posts')"
        ><Icon :icon="Plus" :size="16" class="mr-2" />{{ ui('Choose a post') }}</Button
      >
    </div>
    <section class="overflow-hidden rounded-2xl border border-border bg-card">
      <div class="flex flex-wrap items-center justify-between gap-3 border-b border-border px-6 py-5">
        <div class="flex items-center gap-3">
          <Icon :icon="CalendarDays" :size="19" class="text-primary" />
          <h2 class="text-sm font-semibold">
            {{ formatDate(`${days[0]?.date}T00:00:00Z`) }} —
            {{ formatDate(`${days[6]?.date}T00:00:00Z`) }}
          </h2>
        </div>
        <div class="flex items-center gap-1">
          <Button variant="ghost" size="sm" :aria-label="ui('Previous week')" @click="offset--"
            ><Icon :icon="ChevronLeft" :size="17" /></Button
          ><Button variant="ghost" size="sm" @click="offset = 0">{{ ui('This week') }}</Button
          ><Button variant="ghost" size="sm" :aria-label="ui('Next week')" @click="offset++"
            ><Icon :icon="ChevronRight" :size="17"
          /></Button>
        </div>
      </div>
      <div class="grid grid-cols-1 divide-y divide-border md:grid-cols-7 md:divide-x md:divide-y-0">
        <div
          v-for="day in days"
          :key="day.date"
          :class="['min-h-48 p-3', day.date === today ? 'bg-primary-soft/40' : '']"
        >
          <div class="flex items-center justify-between text-xs">
            <span class="capitalize text-muted-foreground">{{ ui(day.label) }}</span
            ><span
              :class="[
                'flex h-7 w-7 items-center justify-center rounded-full',
                day.date === today ? 'bg-primary text-primary-foreground' : 'font-semibold',
              ]"
              >{{ day.number }}</span
            >
          </div>
          <button
            v-for="item in day.schedules"
            :key="item.id"
            class="mt-3 w-full rounded-lg border border-border bg-card p-2 text-left"
            @click="desk.navigate(`/posts/${item.draftId}`)"
          >
            <p class="truncate text-xs font-semibold">{{ title(item) }}</p>
            <p class="my-2 text-[10px] text-muted-foreground">
              {{ channelNames[item.channel] ?? item.channel }} ·
              {{
                formatDate(item.scheduledAt, {
                  hour: '2-digit',
                  minute: '2-digit',
                  day: undefined,
                  month: undefined,
                })
              }}
            </p>
            <StatusIndicator :status="item.status" />
            <p v-if="changed(item)" class="mt-2 text-[10px] leading-5 text-warning">
              {{ ui('The draft was updated. Open the current revision.') }}
            </p>
          </button>
        </div>
      </div>
    </section>
    <div class="rounded-xl border border-warning/20 bg-warning-soft p-4 text-xs leading-6 text-warning-soft-foreground">
      {{
        ui(
          'For disconnected channels, the saved time is a manual publishing plan. Connecting the channel later does not send earlier plans automatically.',
        )
      }}
    </div>
    <section :class="agenda.length ? 'rounded-2xl border border-border bg-card p-6' : 'query-region'">
      <h2 class="font-editorial text-2xl">{{ ui('All plans') }}</h2>
      <div v-if="agenda.length" class="mt-4 divide-y divide-border">
        <article v-for="item in agenda" :key="item.id" class="flex flex-wrap items-center justify-between gap-4 py-5">
          <div class="min-w-0 flex-1">
            <button
              class="text-left text-sm font-semibold hover:text-primary"
              @click="desk.navigate(`/posts/${item.draftId}`)"
            >
              {{ title(item) }}
            </button>
            <p class="mt-2 text-xs text-muted-foreground">
              {{ channelNames[item.channel] ?? item.channel }} ·
              {{ formatDate(item.scheduledAt, { hour: '2-digit', minute: '2-digit' }) }} · {{ item.draftRevision
              }}{{ ui(' revision') }}
            </p>
            <p v-if="item.message" class="mt-2 max-w-2xl text-xs leading-6 text-muted-foreground">
              {{ ui('Needs attention') }}
            </p>
            <p v-if="item.externalId" class="mt-1 break-all text-xs text-muted-foreground">
              {{ ui('Publication confirmation:') }} {{ item.externalId }}
            </p>
          </div>
          <StatusIndicator :status="item.status" />
          <p v-if="changed(item)" class="mt-2 text-[10px] leading-5 text-warning">
            {{ ui('The draft was updated. Open the current revision.') }}
          </p>
          <Button
            v-if="
              item.status === 'planned' &&
              !workspace?.publicationOperations?.some((operation) => operation.scheduleId === item.id)
            "
            variant="ghost"
            size="sm"
            :is-disabled="desk.busy.value !== null"
            @click="cancel(item.id)"
            ><Icon :icon="X" :size="15" class="mr-1" />{{ ui('Cancel') }}</Button
          >
          <div class="w-full"><DeliveryPanel :plan="item" /></div>
        </article>
      </div>
      <p v-else class="query-label">{{ ui('No plans yet') }}</p>
    </section>
  </div>
</template>
