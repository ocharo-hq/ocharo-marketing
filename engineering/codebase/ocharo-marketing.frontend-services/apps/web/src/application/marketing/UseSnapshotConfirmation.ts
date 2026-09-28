import { computed, ref, watch } from 'vue';
import { Guid } from '@wow-two-beta/ui-vue/foundation/identifiers';

/** Binds explicit operator confirmation to one unchanged review snapshot. */
export function useSnapshotConfirmation(currentKey: () => string | null, isEligible: () => boolean) {
  const isOpen = ref(false);
  const acknowledged = ref(false);
  const reviewedKey = ref<string | null>(null);
  const operationId = ref<string | null>(null);
  const invalidated = ref(false);
  /** Confirmation never survives a changed source, destination, or readiness state. */
  const canConfirm = computed(
    () =>
      isOpen.value &&
      acknowledged.value &&
      !invalidated.value &&
      reviewedKey.value !== null &&
      reviewedKey.value === currentKey() &&
      isEligible(),
  );
  watch(
    currentKey,
    (value) => {
      if (isOpen.value && value !== reviewedKey.value) {
        acknowledged.value = false;
        invalidated.value = true;
      }
    },
    { flush: 'sync' },
  );
  watch(
    isEligible,
    (eligible) => {
      if (!eligible) acknowledged.value = false;
    },
    { flush: 'sync' },
  );

  /** Starts a new reviewed intent; it never silently renews an existing authorization. */
  function begin() {
    const key = currentKey();
    if (key === null) return false;
    isOpen.value = true;
    reviewedKey.value = key;
    acknowledged.value = false;
    invalidated.value = false;
    operationId.value = Guid.createV4();
    return true;
  }
  /** Clears a dismissed intent without issuing any server command. */
  function dismiss() {
    isOpen.value = false;
    acknowledged.value = false;
    reviewedKey.value = null;
    operationId.value = null;
    invalidated.value = false;
  }
  /** Returns the same local command identity across retries of this unchanged reviewed intent. */
  function authorizedOperationId(): string | null {
    return canConfirm.value ? operationId.value : null;
  }
  return {
    isOpen,
    acknowledged,
    reviewedKey,
    operationId,
    invalidated,
    canConfirm,
    begin,
    dismiss,
    authorizedOperationId,
  };
}
