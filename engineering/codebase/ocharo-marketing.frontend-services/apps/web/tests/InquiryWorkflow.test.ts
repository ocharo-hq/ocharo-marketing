import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createAppRouter, RouterHistoryMode } from '@wow-two-beta/ui-vue/router';
import InquiriesPage from '../src/presentation/marketing/InquiriesPage.vue';
const { request } = vi.hoisted(() => ({ request: vi.fn<typeof fetch>() }));
vi.mock('@ocharomarketing/connect', () => ({ integrationFetch: request }));
const wrappers: ReturnType<typeof mount>[] = [];
const sample = {
  id: 'enquiry-1',
  name: 'Review reader',
  contactMethod: 'email',
  contact: 'reader@example.test',
  message: 'What are the measurements?',
  sourcePath: '/catalogue/pieces/nimcha/',
  productId: 'nimcha',
  status: 'new',
  revision: 1,
  note: null,
  createdAt: '2026-09-29T12:00:00Z',
  assignedTo: null,
  followUpAt: null,
  nextStep: null,
};
const result = (items = [sample], total = items.length, offset = 0) =>
  Response.json({ items, total, offset, limit: 20, observedAt: '2026-09-29T13:00:00Z' });
beforeEach(() => {
  request.mockReset();
  request.mockResolvedValue(result());
});
afterEach(() => {
  wrappers.splice(0).forEach((w) => w.unmount());
});
async function render() {
  const router = createAppRouter(
    [
      { path: '/', component: InquiriesPage },
      { path: '/elsewhere', component: { template: '<p>Elsewhere</p>' } },
    ],
    { history: RouterHistoryMode.Memory },
  );
  await router.push('/');
  await router.isReady();
  const wrapper = mount(InquiriesPage, { global: { plugins: [router] } });
  wrappers.push(wrapper);
  await flushPromises();
  return { wrapper, router };
}
describe('enquiry follow-up desk', () => {
  it('preserves edits on conflicts and blocks refresh, pagination and navigation', async () => {
    const { wrapper, router } = await render();
    await wrapper.get('textarea[maxlength="3000"]').setValue('Keep my note');
    const refresh = wrapper.findAll('button').find((b) => b.text() === 'Refresh')!;
    expect(refresh.attributes('disabled')).toBeDefined();
    request.mockResolvedValueOnce(Response.json({ detail: 'The enquiry changed. Reload it first.' }, { status: 409 }));
    await wrapper.findAll('form')[1]!.trigger('submit');
    await flushPromises();
    expect((wrapper.get('textarea[maxlength="3000"]').element as HTMLTextAreaElement).value).toBe('Keep my note');
    expect(wrapper.text()).toContain('Your changes stay here');
    await router.push('/elsewhere');
    await flushPromises();
    expect(router.currentRoute.value.path).toBe('/');
  });
  it('sends explicit follow-up details with local time converted to UTC', async () => {
    const { wrapper } = await render();
    await wrapper.get('input[maxlength="100"]').setValue('Atelier');
    await wrapper.get('input[type="datetime-local"]').setValue('2026-10-01T15:30');
    await wrapper.get('textarea[maxlength="500"]').setValue('Confirm dimensions');
    request.mockImplementationOnce(async (_url, init) => {
      const body = JSON.parse(String(init?.body));
      return Response.json({
        ...sample,
        revision: 2,
        assignedTo: body.followUp.assignedTo,
        followUpAt: body.followUp.followUpAt,
        nextStep: body.followUp.nextStep,
      });
    });
    await wrapper.findAll('form')[1]!.trigger('submit');
    await flushPromises();
    const body = JSON.parse(String(request.mock.calls[1]![1]?.body));
    expect(body.followUp).toEqual({
      assignedTo: 'Atelier',
      followUpAt: new Date('2026-10-01T15:30').toISOString(),
      nextStep: 'Confirm dimensions',
    });
    expect(body).not.toHaveProperty('actor');
    expect(body.expectedRevision).toBe(1);
    expect(wrapper.text()).toContain('No outgoing message or order was created');
    expect(wrapper.text()).not.toContain('Unsaved changes stay');
  });
  it('searches due enquiries and pages without truncating the first hundred', async () => {
    request.mockResolvedValueOnce(
      result(
        Array.from({ length: 20 }, (_, i) => ({ ...sample, id: `enquiry-${i}` })),
        125,
      ),
    );
    const { wrapper } = await render();
    request.mockResolvedValueOnce(result([{ ...sample, id: 'next' }], 125, 20));
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'Next')!
      .trigger('click');
    await flushPromises();
    expect(String(request.mock.calls[1]![0])).toContain('offset=20');
    await wrapper.get('input[type="search"]').setValue('Atelier');
    await wrapper.get('select[aria-label="Follow-up filter"]').setValue('due');
    request.mockResolvedValueOnce(result());
    await wrapper.findAll('form')[0]!.trigger('submit');
    await flushPromises();
    const url = String(request.mock.calls[2]![0]);
    expect(url).toContain('offset=0');
    expect(url).toContain('followUp=due');
    expect(url).toContain('search=Atelier');
  });
  it('loads immutable history with server actor and earlier-page navigation', async () => {
    const { wrapper } = await render();
    const entry = {
      revision: 3,
      recordedAt: '2026-09-29T13:00:00Z',
      actor: 'trusted-member',
      status: 'contacted',
      note: 'Measurements requested',
      assignedTo: 'Atelier',
      followUpAt: null,
      nextStep: 'Reply',
    };
    request.mockResolvedValueOnce(
      Response.json({ total: 2, offset: 0, limit: 20, items: [entry], nextBeforeRevision: 3 }),
    );
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'View activity')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Recorded by trusted-member');
    request.mockResolvedValueOnce(
      Response.json({ total: 2, offset: 1, limit: 20, items: [{ ...entry, revision: 2, note: 'Earlier note' }] }),
    );
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'Earlier activity')!
      .trigger('click');
    await flushPromises();
    expect(String(request.mock.calls[2]![0])).toContain('&beforeRevision=3');
    expect(wrapper.text()).toContain('Earlier note');
    expect(wrapper.text()).toContain('Measurements requested');
  });
  it('discards only when explicitly requested and preserves the view after failed refresh', async () => {
    const { wrapper } = await render();
    await wrapper.get('textarea[maxlength="3000"]').setValue('Draft');
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'Discard unsaved changes')!
      .trigger('click');
    expect((wrapper.get('textarea[maxlength="3000"]').element as HTMLTextAreaElement).value).toBe('');
    request.mockRejectedValueOnce(new Error('Network unavailable'));
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'Refresh')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Review reader');
    expect(wrapper.text()).toContain('Network unavailable');
  });
  it('serializes history reads with writes so older activity cannot overwrite a saved revision', async () => {
    const { wrapper } = await render();
    await wrapper.get('textarea[maxlength="3000"]').setValue('New note');
    let complete!: (value: Response) => void;
    request.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          complete = resolve;
        }),
    );
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'View activity')!
      .trigger('click');
    expect(
      wrapper
        .findAll('button')
        .find((b) => b.text() === 'Save update')!
        .attributes('disabled'),
    ).toBeDefined();
    await wrapper.findAll('form')[1]!.trigger('submit');
    expect(request).toHaveBeenCalledTimes(2);
    complete(Response.json({ total: 0, offset: 0, limit: 20, items: [] }));
    await flushPromises();
    request.mockResolvedValueOnce(Response.json({ ...sample, revision: 2, note: 'New note' }));
    request.mockResolvedValueOnce(
      Response.json({
        total: 1,
        offset: 0,
        limit: 20,
        items: [
          {
            revision: 2,
            recordedAt: '2026-09-29T14:00:00Z',
            actor: 'member',
            status: 'new',
            note: 'New note',
            assignedTo: null,
            followUpAt: null,
            nextStep: null,
          },
        ],
      }),
    );
    await wrapper.findAll('form')[1]!.trigger('submit');
    await flushPromises();
    expect(request).toHaveBeenCalledTimes(4);
    expect(wrapper.text()).toContain('Revision 2');
    expect(wrapper.text()).not.toContain('No recorded changes yet');
  });
});
