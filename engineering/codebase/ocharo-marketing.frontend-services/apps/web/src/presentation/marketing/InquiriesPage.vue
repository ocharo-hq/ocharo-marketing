<script setup lang="ts">
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import { requestFailureMessage } from '@ocharomarketing/connect/RequestFeedback';
import { useErrorToast } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { integrationFetch } from '@ocharomarketing/connect';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { SkeletonState } from '@wow-two-beta/fe/presentation/feedback';
import OperationsUnsavedGuard from './OperationsUnsavedGuard.vue';
const { t: ui, formatWhen } = useUiLocale();

interface Inquiry {
  id: string;
  name: string;
  contactMethod: string;
  contact: string;
  message: string;
  sourcePath: string;
  productId: string | null;
  status: string;
  revision: number;
  note: string | null;
  createdAt: string;
  assignedTo: string | null;
  followUpAt: string | null;
  nextStep: string | null;
}
interface Activity {
  revision: number;
  recordedAt: string;
  actor: string;
  status: string;
  note: string | null;
  assignedTo: string | null;
  followUpAt: string | null;
  nextStep: string | null;
}
interface EditableInquiry extends Inquiry {
  followUpLocal: string;
}
const items = ref<EditableInquiry[]>([]),
  baseline = ref<Record<string, EditableInquiry>>({});
const activity = ref<Record<string, { total: number; items: Activity[]; nextBeforeRevision: number | null }>>({});
const activityOpen = ref<Record<string, boolean>>({}),
  activityBusy = ref<string | null>(null);
const error = ref(''),
  notice = ref(''),
  filter = ref(''),
  search = ref(''),
  followUp = ref('');
const busy = ref(false),
  saving = ref<string | null>(null),
  offset = ref(0),
  total = ref(0);
const observedAt = ref('');
const hasLoaded = ref(false);
// Receipts protect retry revisions without changing the confirmed grid projection.
const writeReceipts = new Map<string, number>();
useErrorToast(error);
const pageSize = 20;
const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
const abort = new AbortController();
let disposed = false;
function localTime(value: string | null) {
  if (!value) return '';
  const date = new Date(value),
    pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
function editable(item: Inquiry): EditableInquiry {
  return {
    ...item,
    assignedTo: item.assignedTo ?? null,
    followUpAt: item.followUpAt ?? null,
    nextStep: item.nextStep ?? null,
    followUpLocal: localTime(item.followUpAt),
  };
}
const changed = (item: EditableInquiry) => JSON.stringify(item) !== JSON.stringify(baseline.value[item.id]);
const dirty = computed(() => items.value.some(changed));
const locked = computed(() => busy.value || saving.value !== null || activityBusy.value !== null);
const formatTime = (value: string) => formatWhen(value);
const due = (item: Inquiry) =>
  item.status !== 'closed' &&
  item.followUpAt !== null &&
  new Date(item.followUpAt).getTime() <= new Date(observedAt.value).getTime();
async function load(nextOffset = 0, savedId?: string) {
  if (!savedId && (dirty.value || locked.value)) return;
  busy.value = true;
  try {
    const params = new URLSearchParams({
      limit: String(pageSize),
      offset: String(nextOffset),
      status: filter.value,
      search: search.value.trim(),
      followUp: followUp.value,
    });
    const r = await integrationFetch('/api/marketing/inquiries?' + params, { signal: abort.signal });
    if (!r.ok) {
      error.value = requestFailureMessage({ code: 'http', status: r.status });
      return false;
    }
    const d = await r.json();
    if (disposed) return;
    const incoming = (d.items as Inquiry[]).map(editable);
    for (const item of incoming) if (item.revision >= (writeReceipts.get(item.id) ?? 0)) writeReceipts.delete(item.id);
    const retained = items.value.filter((item) => item.id !== savedId && changed(item));
    const oldBaseline = baseline.value;
    items.value = incoming.map((item) => retained.find((draft) => draft.id === item.id) ?? item);
    baseline.value = Object.fromEntries(
      incoming.map((item) => [
        item.id,
        retained.some((draft) => draft.id === item.id) ? oldBaseline[item.id]! : { ...item },
      ]),
    );
    hasLoaded.value = true;
    total.value = d.total;
    offset.value = d.offset;
    observedAt.value = d.observedAt;
    if (!savedId) {
      activity.value = {};
      activityOpen.value = {};
    }
    error.value = '';
    return true;
  } catch {
    if (!disposed) error.value = requestFailureMessage({ code: 'transport', status: 0 });
  } finally {
    if (!disposed) busy.value = false;
  }
}
function discard() {
  if (locked.value) return;
  items.value = items.value.map((item) => ({ ...baseline.value[item.id]! }));
  notice.value = 'Unsaved changes discarded. Refresh to load the latest saved version.';
}
async function loadActivity(item: Inquiry, more = false, afterSave = false) {
  if (activityBusy.value !== null || busy.value || (saving.value !== null && !afterSave)) return;
  const cursor = more ? activity.value[item.id]?.nextBeforeRevision : null;
  if (more && cursor == null) return;
  activityOpen.value[item.id] = true;
  activityBusy.value = item.id;
  try {
    const previous = more ? (activity.value[item.id]?.items ?? []) : [];
    const r = await integrationFetch(
      `/api/marketing/inquiries/${encodeURIComponent(item.id)}/activity?limit=20${cursor == null ? '' : '&beforeRevision=' + cursor}`,
      { signal: abort.signal },
    );
    if (!r.ok) {
      error.value = requestFailureMessage({ code: 'http', status: r.status });
      return;
    }
    const d = await r.json();
    if (!disposed)
      activity.value[item.id] = {
        total: d.total,
        items: [...previous, ...d.items],
        nextBeforeRevision: d.nextBeforeRevision ?? null,
      };
  } catch {
    if (!disposed) error.value = requestFailureMessage({ code: 'transport', status: 0 });
  } finally {
    if (!disposed) activityBusy.value = null;
  }
}
async function save(item: EditableInquiry) {
  if (locked.value || !changed(item)) return;
  error.value = '';
  notice.value = '';
  let followUpAt: string | null = null;
  if (item.followUpLocal) {
    const date = new Date(item.followUpLocal);
    if (!Number.isFinite(date.getTime()) || localTime(date.toISOString()) !== item.followUpLocal) {
      error.value = 'Choose a valid local follow-up date and time.';
      return;
    }
    followUpAt = date.toISOString();
  }
  saving.value = item.id;
  try {
    const r = await integrationFetch('/api/marketing/inquiries/' + encodeURIComponent(item.id), {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      signal: abort.signal,
      body: JSON.stringify({
        expectedRevision: writeReceipts.get(item.id) ?? item.revision,
        status: item.status,
        note: item.note,
        followUp: { assignedTo: item.assignedTo, followUpAt, nextStep: item.nextStep },
      }),
    });
    if (!r.ok) {
      error.value = requestFailureMessage({ code: 'http', status: r.status });
      return;
    }
    if (disposed) return;
    const receipt: unknown = await r.json();
    if (
      receipt &&
      typeof receipt === 'object' &&
      'id' in receipt &&
      receipt.id === item.id &&
      'revision' in receipt &&
      Number.isSafeInteger(receipt.revision)
    )
      writeReceipts.set(item.id, Number(receipt.revision));
    if (!(await load(offset.value, item.id))) {
      error.value = 'Saved successfully. The current view could not refresh. Your local edits are preserved.';
      return;
    }
    notice.value = 'Follow-up saved. No outgoing message or order was created.';
    if (activityOpen.value[item.id]) await loadActivity(item, false, true);
  } catch {
    if (!disposed) error.value = requestFailureMessage({ code: 'transport', status: 0 });
  } finally {
    if (!disposed) saving.value = null;
  }
}
onMounted(() => load());
onBeforeUnmount(() => {
  disposed = true;
  abort.abort();
});
</script>
<template>
  <section class="space-y-5">
    <header>
      <p class="text-xs uppercase tracking-widest text-muted-foreground">{{ ui('Ocharo enquiries') }}</p>
      <h1 class="font-editorial text-4xl mt-2">{{ ui('Conversations to continue') }}</h1>
      <p class="mt-3 text-muted-foreground">
        {{ ui('Assign the next conversation, record its outcome, and keep a follow-up date.') }}
      </p>
    </header>
    <form class="flex flex-wrap items-end gap-3 rounded-2xl border border-border bg-card p-4" @submit.prevent="load(0)">
      <fieldset :disabled="locked || dirty" class="contents">
        <label class="block w-full min-w-0 basis-full text-xs font-semibold sm:w-auto sm:flex-1 sm:basis-auto"
          >{{ ui('Search enquiries') }}
          <input
            v-model="search"
            type="search"
            maxlength="200"
            class="mt-2 block min-h-11 w-full rounded-xl border border-border bg-background px-3 text-sm"
            :placeholder="ui('Name, contact, piece, owner or note')"
          />
        </label>
        <label class="block text-xs font-semibold"
          >{{ ui('Status') }}
          <MarketingSelect
            v-model="filter"
            class="mt-2 block"
            :label="ui('Enquiry status')"
            :options="[
              { value: '' as const, label: ui('All statuses') },
              { value: 'new' as const, label: ui('new') },
              { value: 'contacted' as const, label: ui('contacted') },
              { value: 'qualified' as const, label: ui('qualified') },
              { value: 'closed' as const, label: ui('closed') },
            ]"
            :disabled="locked || dirty"
          />
        </label>
        <label class="block text-xs font-semibold"
          >{{ ui('Follow-up') }}
          <MarketingSelect
            v-model="followUp"
            class="mt-2 block"
            :label="ui('Follow-up filter')"
            :options="[
              { value: '' as const, label: ui('All follow-ups') },
              { value: 'due' as const, label: ui('Due now') },
              { value: 'scheduled' as const, label: ui('Scheduled') },
              { value: 'unassigned' as const, label: ui('Unassigned') },
            ]"
            :disabled="locked || dirty"
          />
        </label>
        <Button type="submit" :is-disabled="locked || dirty">{{ ui('Apply filters') }}</Button>
        <Button
          type="button"
          variant="outline"
          :is-disabled="locked || dirty"
          :is-loading="busy"
          @click="load(offset)"
          >{{ ui('Refresh') }}</Button
        >
      </fieldset>
    </form>
    <div v-if="dirty" class="rounded-xl border border-border bg-muted p-4 text-sm" role="status">
      <p>{{ ui('Unsaved changes stay in this view. Save them before searching or changing pages.') }}</p>
      <Button class="mt-3" variant="outline" :is-disabled="locked" @click="discard">{{
        ui('Discard unsaved changes')
      }}</Button>
    </div>
    <p v-if="notice" role="status" class="rounded-xl bg-success-soft p-4 text-sm text-success-soft-foreground">
      {{ ui(notice) }}
    </p>
    <div v-if="hasLoaded || busy" class="flex flex-wrap items-center justify-between gap-3 text-sm">
      <p v-if="hasLoaded && !busy" aria-live="polite">
        {{ ui('{count} matching enquiries', { count: total }) }}
      </p>
      <SkeletonState v-else-if="busy" class="h-5 w-44" />
      <p v-if="observedAt" class="text-xs text-muted-foreground">
        {{ ui('Due as of') }} {{ formatTime(observedAt) }} · {{ timeZone }}
      </p>
    </div>
    <p v-if="!busy && !hasLoaded" class="query-label" role="status">{{ ui('Enquiries unavailable') }}</p>
    <p v-if="hasLoaded && !items.length" class="query-label">
      {{ ui('No enquiries in this view.') }}
    </p>
    <div v-if="busy && !hasLoaded" aria-busy="true" class="query-region space-y-4">
      <div
        v-for="card in 3"
        :key="card"
        class="rounded-2xl border border-border bg-card p-5 sm:p-6 space-y-4"
        aria-hidden="true"
      >
        <SkeletonState class="h-8 w-1/3" /><SkeletonState class="h-5 w-2/3" /><SkeletonState class="h-16 w-full" />
        <div class="grid gap-3 sm:grid-cols-2">
          <SkeletonState class="h-10 w-full" /><SkeletonState class="h-10 w-full" />
        </div>
        <SkeletonState class="h-24 w-full" />
      </div>
    </div>
    <article
      v-for="item in items"
      :key="item.id"
      :aria-busy="busy"
      class="min-w-0 rounded-2xl border border-border bg-card p-5 sm:p-6"
    >
      <div class="flex flex-wrap justify-between gap-3">
        <h2 class="font-editorial text-2xl">{{ item.name }}</h2>
        <time class="break-words text-xs text-muted-foreground">{{ formatTime(item.createdAt) }}</time>
      </div>
      <p class="mt-2 break-words font-semibold">{{ item.contactMethod }} · {{ item.contact }}</p>
      <SkeletonState v-if="busy" class="my-4 h-12 w-full" />
      <p v-else class="my-4 whitespace-pre-wrap break-words">{{ item.message }}</p>
      <p class="break-words text-xs text-muted-foreground">
        {{ item.sourcePath }} · {{ item.productId ?? ui('General enquiry') }} {{ ui('· Reference') }} {{ item.id }}
      </p>
      <p v-if="due(item)" class="mt-3 text-sm font-semibold text-primary">
        {{ ui('Follow-up due ·') }} {{ formatTime(item.followUpAt!) }}
      </p>
      <form class="mt-5" @submit.prevent="save(item)">
        <fieldset :disabled="locked" class="grid gap-4 sm:grid-cols-2">
          <label class="block min-w-0 text-xs font-semibold"
            >{{ ui('Status') }}
            <MarketingSelect
              v-model="item.status"
              class="mt-2 block w-full"
              :label="ui('Status')"
              :options="[
                { value: 'new' as const, label: ui('new') },
                { value: 'contacted' as const, label: ui('contacted') },
                { value: 'qualified' as const, label: ui('qualified') },
                { value: 'closed' as const, label: ui('closed') },
              ]"
              :disabled="locked"
            />
          </label>
          <label class="block min-w-0 text-xs font-semibold"
            >{{ ui('Follow-up owner (name)') }}
            <input
              v-model="item.assignedTo"
              maxlength="100"
              class="mt-2 block min-h-11 w-full rounded-xl border border-border bg-background px-3 text-sm"
              :placeholder="ui('Assign a person')"
            />
          </label>
          <label class="block min-w-0 text-xs font-semibold"
            >{{ ui('Follow-up date ·') }} {{ timeZone }}
            <input
              v-model="item.followUpLocal"
              type="datetime-local"
              min="2000-01-01T00:00"
              max="2100-12-31T23:59"
              class="mt-2 block min-h-11 w-full min-w-0 rounded-xl border border-border bg-background px-3 text-sm"
            />
            <span class="mt-2 block font-normal text-muted-foreground">{{
              ui('Optional. Clear this date before closing.')
            }}</span>
          </label>
          <label class="block min-w-0 text-xs font-semibold"
            >{{ ui('Next step') }}
            <textarea
              v-model="item.nextStep"
              maxlength="500"
              rows="2"
              class="mt-2 block w-full rounded-xl border border-border bg-background px-3 py-2.5 text-sm"
              :placeholder="ui('What should this person do?')"
            />
          </label>
          <label class="block min-w-0 text-xs font-semibold sm:col-span-2"
            >{{ ui('Internal note') }}
            <textarea
              v-model="item.note"
              maxlength="3000"
              rows="3"
              class="mt-2 block w-full rounded-xl border border-border bg-background px-3 py-2.5 text-sm"
            />
          </label>
          <Button
            type="submit"
            class="justify-self-start"
            :is-loading="saving === item.id"
            :is-disabled="locked || !changed(item)"
            >{{ ui('Save update') }}</Button
          >
        </fieldset>
      </form>
      <div class="mt-5 border-t border-border pt-4">
        <Button
          variant="outline"
          :is-disabled="locked"
          :aria-expanded="activityOpen[item.id] ?? false"
          @click="activityOpen[item.id] ? (activityOpen[item.id] = false) : loadActivity(item)"
          >{{ activityOpen[item.id] ? ui('Hide activity') : ui('View activity') }}</Button
        >
        <Button
          v-if="activityOpen[item.id]"
          class="mt-4 ml-3"
          variant="outline"
          size="sm"
          :is-disabled="locked"
          :is-loading="activityBusy === item.id"
          @click="loadActivity(item)"
          >{{ ui('Refresh') }}</Button
        >
        <div v-if="activityOpen[item.id]" class="mt-4 space-y-3">
          <div
            v-if="activityBusy === item.id && !activity[item.id]"
            aria-busy="true"
            :aria-label="ui('Enquiry activity')"
            class="space-y-3"
          >
            <div v-for="row in 2" :key="row" class="rounded-xl bg-muted p-4 space-y-2" aria-hidden="true">
              <SkeletonState class="h-5 w-2/3" /><SkeletonState class="h-4 w-1/2" /><SkeletonState class="h-5 w-full" />
            </div>
          </div>
          <p v-else-if="!activity[item.id]" class="query-label">{{ ui('Activity unavailable') }}</p>
          <p v-else-if="!activity[item.id]?.items.length" class="query-label">
            {{ ui('No recorded changes yet. Earlier notes remain on the enquiry.') }}
          </p>
          <ol :aria-busy="activityBusy === item.id" class="space-y-3">
            <li v-for="entry in activity[item.id]?.items" :key="entry.revision" class="rounded-xl bg-muted p-4 text-sm">
              <p class="break-words font-semibold">
                {{ ui('Revision') }} {{ entry.revision }} · {{ ui(entry.status) }} · {{ formatTime(entry.recordedAt) }}
              </p>
              <p class="mt-1 break-words text-xs text-muted-foreground">
                {{ ui('Recorded by') }} {{ entry.actor }} {{ ui('· Owner:') }}
                {{ entry.assignedTo || ui('Unassigned') }}
              </p>
              <p v-if="entry.followUpAt" class="mt-2">{{ ui('Follow-up:') }} {{ formatTime(entry.followUpAt) }}</p>
              <p v-if="entry.nextStep" class="mt-2 whitespace-pre-wrap break-words">
                {{ ui('Next:') }} {{ entry.nextStep }}
              </p>
              <SkeletonState v-if="activityBusy === item.id && entry.note" class="mt-2 h-5 w-full" />
              <p v-else-if="entry.note" class="mt-2 whitespace-pre-wrap break-words">{{ entry.note }}</p>
            </li>
          </ol>
          <Button
            v-if="activity[item.id]?.nextBeforeRevision != null"
            variant="outline"
            :is-disabled="locked"
            @click="loadActivity(item, true)"
            >{{ ui('Earlier activity') }}</Button
          >
        </div>
      </div>
    </article>
    <nav
      v-if="total > pageSize || offset > 0"
      :aria-label="ui('Enquiry pages')"
      class="flex flex-wrap items-center justify-between gap-3"
    >
      <Button
        variant="outline"
        :is-disabled="locked || dirty || offset === 0"
        @click="load(Math.max(0, offset - pageSize))"
        >{{ ui('Previous') }}</Button
      >
      <p class="text-sm">
        {{ Math.min(offset + 1, total) }}–{{ Math.min(offset + items.length, total) }} {{ ui('of') }} {{ total }}
      </p>
      <Button
        variant="outline"
        :is-disabled="locked || dirty || offset + items.length >= total"
        @click="load(offset + pageSize)"
        >{{ ui('Next') }}</Button
      >
    </nav>
    <OperationsUnsavedGuard :dirty="dirty || saving !== null" />
  </section>
</template>
