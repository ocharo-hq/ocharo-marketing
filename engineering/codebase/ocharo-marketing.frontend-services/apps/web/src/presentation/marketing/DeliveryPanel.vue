<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, ref } from 'vue';
import { Send, ShieldCheck, RefreshCw } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { CheckboxInput } from '@wow-two-beta/ui-vue/presentation/forms';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalDescription,
  ModalFooter,
} from '@wow-two-beta/ui-vue/presentation/overlays';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import StatusIndicator from './StatusIndicator.vue';
import { useMarketingWorkspace, useSnapshotConfirmation } from '../../application/marketing';
import {
  DeliveryExtensions,
  type ScheduleModel,
  type DraftModel,
  type ConnectionModel,
  type AssetModel,
} from '../../domain/marketing';
const { t: ui, formatDate } = useUiLocale();

/** Reviews a future plan, then displays the authoritative frozen delivery record. */
defineOptions({ name: 'DeliveryPanel' });
const props = defineProps<{ plan: ScheduleModel }>();
const desk = useMarketingWorkspace();
const operation = computed(() =>
  desk.workspace.value?.publicationOperations?.find((item) => item.scheduleId === props.plan.id),
);
const currentConnection = computed(() =>
  desk.workspace.value?.connections.find((item) => item.id === operation.value?.connectionId),
);
const canReconcile = computed(
  () =>
    operation.value?.canReconcile === true &&
    currentConnection.value?.verificationStatus === 'verified' &&
    currentConnection.value.capabilities.includes('public-metrics') &&
    currentConnection.value.accountId === operation.value.snapshot.identity.accountId &&
    currentConnection.value.destinationId === operation.value.snapshot.identity.destinationId &&
    desk.workspace.value?.deliverySettings?.metricReadsEnabled === true &&
    desk.workspace.value?.connections.find((item) => item.id === operation.value?.connectionId)?.metricReadsEnabled ===
      true,
);
/** A receipt check is a separately authorized provider read and never a repost. */
async function reconcile() {
  if (!canReconcile.value) return;
  const result = await desk.reconcilePublication(props.plan.id);
  if (result.ok) desk.notice.value = 'Saqlangan kanal tasdig‘i tekshirildi. Post qayta yuborilmadi.';
}
const reasons = computed(() => DeliveryExtensions.blockers(desk.workspace.value, props.plan));
const confirmation = useSnapshotConfirmation(
  () => DeliveryExtensions.reviewKey(desk.workspace.value, props.plan),
  () => reasons.value.length === 0,
);
const review = ref<{ draft: DraftModel; connection: ConnectionModel; assets: AssetModel[]; dueAt: string } | null>(
  null,
);
/** Captures exactly what is shown; current changes invalidate the separate acknowledgement. */
function beginReview() {
  const ws = desk.workspace.value;
  const draft = ws?.drafts.find((item) => item.id === props.plan.draftId);
  const connection = ws?.connections.find((item) => item.id === props.plan.channel);
  if (!ws || !draft || !connection || reasons.value.length) return;
  review.value = structuredClone({
    draft,
    connection,
    assets: draft.mediaIds.flatMap((id) => ws.assets.filter((asset) => asset.id === id)),
    dueAt: props.plan.scheduledAt,
  });
  confirmation.begin();
}
/** One reviewed intent retains its operation ID after a response failure. */
async function queue() {
  const operationId = confirmation.authorizedOperationId();
  const currentReasons = DeliveryExtensions.blockers(desk.workspace.value, props.plan);
  if (currentReasons.length) {
    desk.error.value = currentReasons.join(' ');
    return;
  }
  if (!operationId || !review.value) return;
  const result = await desk.queuePublication(props.plan.id, {
    expectedDraftRevision: review.value.draft.revision,
    connectionId: review.value.connection.id,
    expectedConnectionRevision: review.value.connection.connectionRevision ?? 0,
    operationId,
  });
  if (result.ok) {
    confirmation.dismiss();
    desk.notice.value = 'Tekshirilgan post va manzil yuborish navbatiga saqlandi.';
  }
}
/** Cancellation never claims to remove a post already delivered to a provider. */
async function cancel() {
  const result = await desk.cancelSchedule(props.plan.id);
  if (result.ok) desk.notice.value = 'Yuborish navbati bekor qilindi.';
}
</script>
<template>
  <div class="mt-4 rounded-xl border border-border bg-muted/30 p-4">
    <template v-if="operation">
      <div class="flex flex-wrap items-center gap-3">
        <Icon :icon="ShieldCheck" :size="16" class="text-primary" /><span class="text-xs font-semibold">{{
          ui('Saved delivery revision')
        }}</span
        ><StatusIndicator :status="operation.status" />
      </div>
      <p class="mt-3 text-sm font-semibold">
        {{ operation.snapshot.draft.title }} · {{ operation.snapshot.draft.revision }}{{ ui(' revision') }}
      </p>
      <p class="mt-2 break-words text-xs leading-6">
        {{ operation.snapshot.identity.displayName }} · {{ operation.snapshot.identity.provider }}
      </p>
      <p class="break-all text-[11px] text-muted-foreground">
        {{ ui('Account:') }} {{ operation.snapshot.identity.accountId }} {{ ui('· Destination:') }}
        {{ operation.snapshot.identity.destinationId }} · {{ operation.snapshot.identity.revision
        }}{{ ui(' connection') }}
      </p>
      <p class="mt-2 text-xs text-muted-foreground">
        {{ formatDate(operation.dueAt, { hour: '2-digit', minute: '2-digit' }) }} {{ ui('· Tashkent') }}
      </p>
      <details class="mt-3 text-xs">
        <summary class="cursor-pointer font-semibold text-primary">
          {{ ui('The exact text and images to send') }}
        </summary>
        <p class="mt-3 whitespace-pre-wrap break-words leading-6">
          {{ operation.snapshot.draft.caption || ui('No text entered.') }}
        </p>
        <div class="mt-3 flex flex-wrap gap-3">
          <div v-for="media in operation.snapshot.media" :key="media.assetId" class="max-w-36">
            <img
              :src="`/api/marketing/assets/${encodeURIComponent(media.assetId)}`"
              :alt="media.fileName"
              class="h-32 w-28 rounded-lg bg-card object-contain"
            />
            <p class="mt-1 break-all text-[10px]">{{ media.fileName }}</p>
            <details class="text-[10px] text-muted-foreground">
              <summary>{{ ui('Source provenance') }}</summary>
              <p class="break-all">SHA-256: {{ media.sha256 }}</p>
            </details>
          </div>
        </div>
      </details>
      <p v-if="operation.message" class="mt-3 text-xs leading-6 text-muted-foreground">{{ ui('Needs attention') }}</p>
      <p v-if="operation.receipt" class="mt-3 break-all text-xs">
        {{ ui('Channel confirmation:') }} {{ operation.receipt.externalId }} · {{ ui(operation.receipt.stage) }}
      </p>
      <p v-if="['unknown', 'held', 'revoked'].includes(operation.status)" class="mt-3 text-xs leading-6 text-warning">
        {{
          operation.status === 'unknown'
            ? ui('Unknown outcome. Check the channel history manually; this operation will not be sent again.')
            : ui('This request cannot be re-enabled. Check the connection and create a new plan.')
        }}
      </p>
      <p class="mt-2 text-[11px] leading-6 text-muted-foreground">{{ ui(operation.reconciliationMessage) }}</p>
      <div v-if="operation.canReconcile" class="mt-3">
        <Button
          size="sm"
          variant="outline"
          :is-disabled="!canReconcile || desk.busy.value !== null"
          :is-loading="desk.busy.value === 'reconcile'"
          @click="reconcile"
          >{{ ui('Check channel confirmation') }}</Button
        >
        <p class="mt-2 text-[11px] leading-6 text-muted-foreground">
          {{ ui('This X read request may use credits. No new post will be sent.') }}
        </p>
      </div>
      <Button
        v-if="operation.canCancel"
        variant="outline"
        size="sm"
        class="mt-3"
        :is-disabled="desk.busy.value !== null"
        @click="cancel"
        >{{ ui('Cancel queue item') }}</Button
      >
    </template>
    <template v-else>
      <div class="flex items-center gap-2 text-xs font-semibold">
        <Icon :icon="Send" :size="15" />{{ ui('Automatic delivery') }}
      </div>
      <ul v-if="reasons.length" class="mt-3 space-y-1 text-xs leading-6 text-muted-foreground">
        <li v-for="reason in reasons" :key="reason">{{ ui(reason) }}</li>
      </ul>
      <p v-else class="mt-3 text-xs leading-6 text-muted-foreground">
        {{ ui('Review the approved post, image and channel destination, then confirm separately.') }}
      </p>
      <div class="mt-3 flex flex-wrap gap-2">
        <Button size="sm" :is-disabled="reasons.length > 0 || desk.busy.value !== null" @click="beginReview">{{
          ui('Review delivery')
        }}</Button
        ><Button variant="ghost" size="sm" :is-disabled="desk.busy.value !== null" @click="desk.refresh()"
          ><Icon :icon="RefreshCw" :size="13" class="mr-1" />{{ ui('Check status') }}</Button
        >
      </div>
    </template>
    <Modal
      :open="confirmation.isOpen.value"
      @update:open="
        (open) => {
          if (!open) confirmation.dismiss();
        }
      "
      ><ModalContent class="max-h-[90vh] max-w-xl overflow-y-auto"
        ><ModalHeader
          ><ModalTitle>{{ ui('Authorize delivery') }}</ModalTitle
          ><ModalDescription>{{
            ui('This confirmation authorizes this exact post to this channel at the scheduled time.')
          }}</ModalDescription></ModalHeader
        >
        <div v-if="review" class="my-5 space-y-4">
          <div class="rounded-xl bg-muted p-4">
            <p class="font-semibold">{{ review.draft.title }} · {{ review.draft.revision }}{{ ui(' revision') }}</p>
            <p class="mt-2 text-sm">{{ review.connection.displayName }} · {{ review.connection.name }}</p>
            <p class="mt-1 break-all text-xs text-muted-foreground">
              {{ ui('Account:') }} {{ review.connection.accountId }} {{ ui('· Destination:') }}
              {{ review.connection.destinationId }}
            </p>
            <p class="mt-2 text-xs">
              {{ formatDate(review.dueAt, { hour: '2-digit', minute: '2-digit' }) }} {{ ui('· Tashkent') }}
            </p>
          </div>
          <p class="whitespace-pre-wrap break-words text-sm leading-7">
            {{ review.draft.caption || ui('No text entered.') }}
          </p>
          <div class="flex flex-wrap gap-3">
            <img
              v-for="asset in review.assets"
              :key="asset.id"
              :src="asset.url"
              :alt="asset.fileName"
              class="h-44 max-w-40 rounded-lg bg-muted object-contain"
            />
          </div>
          <p v-if="!review.assets.length" class="text-xs text-muted-foreground">{{ ui('Only text will be sent.') }}</p>
          <p
            v-if="confirmation.invalidated.value"
            class="rounded-lg bg-warning-soft p-3 text-xs leading-6 text-warning"
          >
            {{ ui('The post, destination or plan changed. Review the updated state.') }}
          </p>
          <label class="flex items-start gap-3 rounded-lg border border-border p-4 text-sm leading-6"
            ><CheckboxInput
              v-model="confirmation.acknowledged.value"
              :disabled="confirmation.invalidated.value || desk.busy.value !== null"
            /><span>{{
              ui('I authorize sending this post and its images to the shown account at the scheduled time.')
            }}</span></label
          >
          <Button variant="ghost" size="sm" :is-disabled="desk.busy.value !== null" @click="desk.refresh()">{{
            ui('Refresh saved state')
          }}</Button>
        </div>
        <ModalFooter
          ><Button variant="outline" :is-disabled="desk.busy.value !== null" @click="confirmation.dismiss">{{
            ui('Save for now')
          }}</Button
          ><Button v-if="confirmation.invalidated.value" :is-disabled="reasons.length > 0" @click="beginReview">{{
            ui('Review again')
          }}</Button
          ><Button
            v-else
            :is-loading="desk.busy.value === 'queue'"
            :is-disabled="!confirmation.canConfirm.value || desk.busy.value !== null"
            @click="queue"
            >{{ ui('Add to delivery queue') }}</Button
          ></ModalFooter
        >
      </ModalContent></Modal
    >
  </div>
</template>
