import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createAppRouter, RouterHistoryMode } from '@wow-two-beta/fe/router';
import { feedbackBus } from '../src/application/shared/UseFeedback';
import InquiriesPage from '../src/presentation/marketing/InquiriesPage.vue';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
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
  note: null as string | null,
  createdAt: '2026-09-29T12:00:00Z',
  assignedTo: null as string | null,
  followUpAt: null as string | null,
  nextStep: null as string | null,
};
const result = (items = [sample], total = items.length, offset = 0) =>
  Response.json({ items, total, offset, limit: 20, observedAt: '2026-09-29T13:00:00Z' });
beforeEach(() => {
  vi.restoreAllMocks();
  vi.spyOn(feedbackBus, 'notify');
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
    expect(wrapper.text()).not.toContain('The enquiry changed.');
    expect(feedbackBus.notify).toHaveBeenCalledWith(
      expect.objectContaining({ description: expect.stringContaining('current state') }),
    );
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
    request.mockResolvedValueOnce(
      result([
        {
          ...sample,
          revision: 2,
          assignedTo: 'Atelier',
          followUpAt: new Date('2026-10-01T15:30').toISOString(),
          nextStep: 'Confirm dimensions',
        },
      ]),
    );
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
    wrapper
      .findAllComponents({ name: 'MarketingSelect' })
      .find((picker) => picker.props('label') === 'Follow-up filter')!
      .vm.$emit('update:modelValue', 'due');
    await flushPromises();
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
      followUpAt: null as string | null,
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
    expect(wrapper.text()).not.toContain('Network unavailable');
    expect(feedbackBus.notify).toHaveBeenCalledWith(
      expect.objectContaining({ description: expect.stringContaining('try again') }),
    );
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
    request.mockResolvedValueOnce(result([{ ...sample, revision: 2, note: 'New note' }]));
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
            assignedTo: null as string | null,
            followUpAt: null as string | null,
            nextStep: null as string | null,
          },
        ],
      }),
    );
    await wrapper.findAll('form')[1]!.trigger('submit');
    await flushPromises();
    expect(request).toHaveBeenCalledTimes(5);
    expect(wrapper.text()).toContain('Revision 2');
    expect(wrapper.text()).not.toContain('No recorded changes yet');
  });
});

describe('server-confirmed enquiry projection', () => {
  it('does not skeleton or adopt mutation payload before acknowledgement, and keeps form mounted during the GET', async () => {
    const { wrapper } = await render();
    await wrapper.get('textarea[maxlength="3000"]').setValue('Local note');
    let acknowledge!: (value: Response) => void;
    let project!: (value: Response) => void;
    request.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          acknowledge = resolve;
        }),
    );
    request.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          project = resolve;
        }),
    );
    await wrapper.findAll('form')[1]!.trigger('submit');
    await flushPromises();
    expect(request).toHaveBeenCalledTimes(2);
    expect(wrapper.find('article[aria-busy="true"]').exists()).toBe(false);
    expect((wrapper.get('textarea[maxlength="3000"]').element as HTMLTextAreaElement).value).toBe('Local note');
    acknowledge(Response.json({ ...sample, revision: 2, note: 'Mutation response must not fill the view' }));
    await flushPromises();
    expect(request).toHaveBeenCalledTimes(3);
    expect(wrapper.find('article[aria-busy="true"]').exists()).toBe(true);
    expect((wrapper.get('textarea[maxlength="3000"]').element as HTMLTextAreaElement).value).toBe('Local note');
    project(result([{ ...sample, revision: 2, note: 'Authoritative read' }]));
    await flushPromises();
    expect(wrapper.find('article[aria-busy="true"]').exists()).toBe(false);
    expect((wrapper.get('textarea[maxlength="3000"]').element as HTMLTextAreaElement).value).toBe('Authoritative read');
  });
});

it('uses a private acknowledged revision on retry after a failed follow-up read', async () => {
  const { wrapper } = await render();
  await wrapper.get('textarea[maxlength="3000"]').setValue('Local saved note');
  request.mockResolvedValueOnce(Response.json({ ...sample, revision: 2, note: 'Local saved note' }));
  request.mockResolvedValueOnce(Response.json({ detail: 'private read failure' }, { status: 503 }));
  await wrapper.findAll('form')[1]!.trigger('submit');
  await flushPromises();
  expect((wrapper.get('textarea[maxlength="3000"]').element as HTMLTextAreaElement).value).toBe('Local saved note');
  expect(wrapper.find('[aria-busy="true"]').exists()).toBe(false);
  request.mockResolvedValueOnce(Response.json({ detail: 'private write failure' }, { status: 503 }));
  await wrapper.findAll('form')[1]!.trigger('submit');
  await flushPromises();
  const retry = JSON.parse(String(request.mock.calls[3]![1]?.body));
  expect(retry.expectedRevision).toBe(2);
  expect(wrapper.text()).not.toContain('private');
});

it('reports a failed first read as unavailable instead of an empty confirmed result', async () => {
  request.mockResolvedValueOnce(Response.json({ detail: 'private unavailable response' }, { status: 503 }));
  const { wrapper } = await render();
  expect(wrapper.text()).toContain('Enquiries unavailable');
  expect(wrapper.text()).not.toContain('matching enquiries');
  expect(wrapper.text()).not.toContain('No enquiries');
  expect(wrapper.text()).not.toContain('private');
  request.mockResolvedValueOnce(result([], 0));
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'Refresh')!
    .trigger('click');
  await flushPromises();
  expect(wrapper.text()).toContain('0 matching enquiries');
  expect(wrapper.text()).toContain('No enquiries in this view.');
});
it('uses a history skeleton then a persistent toolbar refresh after a failed first activity read', async () => {
  const { wrapper } = await render();
  let finish!: (response: Response) => void;
  request.mockImplementationOnce(
    () =>
      new Promise((resolve) => {
        finish = resolve;
      }),
  );
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'View activity')!
    .trigger('click');
  await flushPromises();
  expect(wrapper.find('[aria-busy="true"]').exists()).toBe(true);
  expect(wrapper.text()).not.toContain('Loading activity');
  expect(wrapper.text()).not.toContain('No recorded changes');
  finish(Response.json({ detail: 'private history' }, { status: 503 }));
  await flushPromises();
  expect(wrapper.find('[aria-busy="true"]').exists()).toBe(false);
  expect(wrapper.text()).toContain('Activity unavailable');
  expect(wrapper.text()).not.toContain('No recorded changes');
  expect(wrapper.text()).not.toContain('private');
  request.mockResolvedValueOnce(Response.json({ total: 0, items: [], nextBeforeRevision: null }));
  await wrapper
    .get('article')
    .findAll('button')
    .find((button) => button.text() === 'Refresh')!
    .trigger('click');
  await flushPromises();
  expect(wrapper.text()).toContain('No recorded changes');
});

it('keeps a successful empty region plain during toolbar refresh', async () => {
  request.mockResolvedValueOnce(result([]));
  const { wrapper } = await render();
  expect(wrapper.get('.query-label').text()).toBe('No enquiries in this view.');
  expect(wrapper.get('.query-label').classes()).not.toContain('border');
  let finish!: (value: Response) => void;
  request.mockReturnValueOnce(
    new Promise((resolve) => {
      finish = resolve;
    }),
  );
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'Refresh')!
    .trigger('click');
  expect(wrapper.findAll('article')).toHaveLength(0);
  expect(wrapper.find('.query-region[aria-busy=true]').exists()).toBe(false);
  finish(result([]));
  await flushPromises();
  expect(wrapper.get('.query-label').text()).toBe('No enquiries in this view.');
});
it('keeps enquiry names and form actions mounted while a successful refresh is pending', async () => {
  const { wrapper } = await render();
  let finish!: (value: Response) => void;
  request.mockReturnValueOnce(
    new Promise((resolve) => {
      finish = resolve;
    }),
  );
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'Refresh')!
    .trigger('click');
  expect(wrapper.get('article h2').text()).toBe('Review reader');
  expect(wrapper.get('textarea[maxlength="3000"]').isVisible()).toBe(true);
  expect(wrapper.get('article').attributes('aria-busy')).toBe('true');
  finish(Response.json({}, { status: 503 }));
  await flushPromises();
  expect(wrapper.get('article h2').text()).toBe('Review reader');
  expect(wrapper.text()).not.toContain('Enquiries unavailable');
});

it('keeps enquiry status keys stable when labels are translated', async () => {
  const locale = useUiLocale();
  locale.setUiLocale('ru');
  try {
    const { wrapper } = await render();
    const picker = wrapper
      .findAllComponents({ name: 'MarketingSelect' })
      .find((control) => control.props('modelValue') === 'new')!;
    expect(picker.props('options')).toEqual(expect.arrayContaining([expect.objectContaining({ value: 'contacted' })]));
    picker.vm.$emit('update:modelValue', 'contacted');
    await flushPromises();
    request.mockResolvedValueOnce(Response.json({ ...sample, status: 'contacted', revision: 2 }));
    request.mockResolvedValueOnce(result([{ ...sample, status: 'contacted', revision: 2 }]));
    await wrapper.findAll('form')[1]!.trigger('submit');
    await flushPromises();
    expect(JSON.parse(String(request.mock.calls[1]![1]?.body)).status).toBe('contacted');
  } finally {
    locale.setUiLocale('en');
  }
});
