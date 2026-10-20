import { computed, getCurrentScope, onScopeDispose, shallowReactive, toValue, type MaybeRefOrGetter } from "vue";

/** @internal Every mounted editor's unsaved-or-saving state, as its existing navigation guard reads it. */
const sources = shallowReactive(new Set<MaybeRefOrGetter<boolean>>());

/**
 * True while any mounted editor has unsaved edits or a save in flight. The frame holds sign-out on it, so a sign-out
 * never strands work the operator has not saved; navigation keeps its own guards.
 */
export const unsavedWork = computed(() => [...sources].some((source) => toValue(source)));

/**
 * Registers an editor's guard state — the same value its navigation guard blocks on — for as long as the calling
 * component or effect scope lives.
 */
export function trackUnsavedWork(active: MaybeRefOrGetter<boolean>): void {
  sources.add(active);
  if (getCurrentScope()) onScopeDispose(() => sources.delete(active));
}
