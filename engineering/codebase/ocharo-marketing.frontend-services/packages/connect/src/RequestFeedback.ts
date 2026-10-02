export interface RequestFailure { readonly code: string; readonly status: number; }
export function requestFailureMessage(failure: RequestFailure): string {
  if (failure.code === 'cancelled') return '';
  if (failure.status === 401) return 'Sign in again. Your edits are preserved.';
  if (failure.status === 403) return 'Your access changed. Your edits are preserved.';
  if (failure.status === 409) return 'This operation no longer matches the current state. Your data is preserved; check the status.';
  if ([400, 422].includes(failure.status)) return 'Review the entered values. Your edits are preserved.';
  if (failure.status === 404) return 'This item is unavailable. Your edits are preserved.';
  return 'The request could not complete. Your edits are preserved; try again.';
}
