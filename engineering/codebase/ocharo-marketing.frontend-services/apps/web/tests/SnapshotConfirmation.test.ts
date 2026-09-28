import { effectScope, ref } from 'vue';
import { afterEach, describe, expect, it } from 'vitest';
import { useSnapshotConfirmation } from '../src/application/marketing/UseSnapshotConfirmation';

const scopes: ReturnType<typeof effectScope>[] = [];
afterEach(() => {
  for (const scope of scopes.splice(0)) scope.stop();
});
/** Creates local confirmation state with no provider or HTTP behavior. */
function setup() {
  const key = ref<string | null>('schedule:s1|draft:r3|connection:c2|media:hash1');
  const eligible = ref(true);
  const scope = effectScope();
  scopes.push(scope);
  const confirmation = scope.run(() =>
    useSnapshotConfirmation(
      () => key.value,
      () => eligible.value,
    ),
  )!;
  return { key, eligible, confirmation };
}
describe('explicit snapshot authorization', () => {
  it('requires an explicit acknowledgement before exposing a command identity', () => {
    const { confirmation } = setup();
    expect(confirmation.begin()).toBe(true);
    expect(confirmation.authorizedOperationId()).toBeNull();
    confirmation.acknowledged.value = true;
    expect(confirmation.authorizedOperationId()).toMatch(/^[0-9a-f-]{36}$/);
  });
  it('keeps the same operation identity while retrying an unchanged confirmed command', () => {
    const { confirmation } = setup();
    confirmation.begin();
    confirmation.acknowledged.value = true;
    const first = confirmation.authorizedOperationId();
    expect(confirmation.authorizedOperationId()).toBe(first);
  });
  it('invalidates consent when a source revision or destination changes', () => {
    const { key, confirmation } = setup();
    confirmation.begin();
    confirmation.acknowledged.value = true;
    key.value = 'schedule:s1|draft:r4|connection:c3|media:hash2';
    expect(confirmation.acknowledged.value).toBe(false);
    expect(confirmation.invalidated.value).toBe(true);
    confirmation.acknowledged.value = true;
    expect(confirmation.authorizedOperationId()).toBeNull();
  });
  it('requires a new reviewed intent after invalidation', () => {
    const { key, confirmation } = setup();
    confirmation.begin();
    const previous = confirmation.operationId.value;
    key.value = 'new-snapshot';
    confirmation.begin();
    expect(confirmation.operationId.value).not.toBe(previous);
    expect(confirmation.acknowledged.value).toBe(false);
    expect(confirmation.invalidated.value).toBe(false);
  });
  it('revokes confirmation when execution readiness disappears', () => {
    const { eligible, confirmation } = setup();
    confirmation.begin();
    confirmation.acknowledged.value = true;
    eligible.value = false;
    expect(confirmation.acknowledged.value).toBe(false);
    expect(confirmation.authorizedOperationId()).toBeNull();
    eligible.value = true;
    expect(confirmation.authorizedOperationId()).toBeNull();
  });
  it('does not create an intent without a review snapshot and clears dismissed intent', () => {
    const { key, confirmation } = setup();
    key.value = null;
    expect(confirmation.begin()).toBe(false);
    key.value = 'present';
    confirmation.begin();
    confirmation.acknowledged.value = true;
    confirmation.dismiss();
    expect(confirmation.operationId.value).toBeNull();
    expect(confirmation.authorizedOperationId()).toBeNull();
  });
});
