<script setup lang="ts">
import { computed } from 'vue';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import StatusIndicator from './StatusIndicator.vue';
import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type ConnectionModel } from '../../domain/marketing';
/** Separates configuration, verified destination and execution permission. */
defineOptions({ name: 'ConnectionReadiness' });
const props = defineProps<{ connection: ConnectionModel }>();
const desk = useMarketingWorkspace();
const canVerify = computed(
  () =>
    desk.workspace.value?.deliverySettings?.accountVerificationEnabled === true && props.connection.canVerify === true,
);
/** Verification is an explicit read-only provider call, never a publish action. */
async function verify() {
  if (!canVerify.value) return;
  const result = await desk.verifyConnection(props.connection.id);
  if (result.ok) desk.notice.value = 'Kanalning akkaunt va manzil holati tekshirildi.';
}
</script>
<template>
  <div class="mt-4 rounded-xl border border-border bg-muted/30 p-4">
    <dl class="grid grid-cols-[1fr_auto] gap-x-3 gap-y-3 text-xs">
      <dt>Akkaunt va manzil</dt>
      <dd><StatusIndicator :status="connection.verificationStatus ?? 'not-verified'" /></dd>
      <dt>Avtomatik yuborish</dt>
      <dd>
        {{
          connection.executionEnabled && desk.workspace.value?.deliverySettings?.publishingEnabled
            ? 'Yoqilgan'
            : 'O‘chirilgan'
        }}
      </dd>
      <dt>Ko‘rsatkichlarni o‘qish</dt>
      <dd>
        {{
          connection.metricReadsEnabled && desk.workspace.value?.deliverySettings?.metricReadsEnabled
            ? 'Yoqilgan'
            : 'O‘chirilgan'
        }}
      </dd>
    </dl>
    <div v-if="connection.displayName || connection.destinationId" class="mt-4 border-t border-border pt-3">
      <p class="text-sm font-semibold">{{ connection.displayName }}</p>
      <p class="mt-1 break-all text-xs leading-6 text-muted-foreground">
        Akkaunt: {{ connection.accountId ?? 'Tekshirilmagan' }} · Manzil:
        {{ connection.destinationId ?? 'Tekshirilmagan' }}
      </p>
      <p v-if="connection.verifiedAt" class="mt-1 text-[11px] text-muted-foreground">
        Tekshirildi:
        {{ MarketingExtensions.formatDate(connection.verifiedAt, { hour: '2-digit', minute: '2-digit' }) }} ·
        {{ connection.connectionRevision }}-ulanish
      </p>
    </div>
    <p v-if="connection.safeMessage" class="mt-3 text-xs leading-6 text-muted-foreground">
      {{ connection.safeMessage }}
    </p>
    <Button
      class="mt-4"
      size="sm"
      variant="outline"
      :is-disabled="!canVerify || desk.busy.value !== null"
      :is-loading="desk.busy.value === 'verify'"
      @click="verify"
      >Akkauntni tekshirish</Button
    >
    <p class="mt-2 text-[11px] leading-5 text-muted-foreground">
      {{
        canVerify
          ? 'Bu tugma kanal ma’lumotini o‘qiydi. Post yubormaydi.'
          : 'Tashqi akkaunt tekshiruvi hozir mavjud emas. Sozlanganlik yuborishga ruxsat bermaydi.'
      }}
    </p>
  </div>
</template>
