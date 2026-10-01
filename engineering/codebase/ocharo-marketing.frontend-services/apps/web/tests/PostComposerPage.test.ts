import { flushPromises, mount } from '@vue/test-utils';
import { shallowRef, ref } from 'vue';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiFailureFactory } from '@wow-two-beta/ui-vue/foundation/http';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import { createAppRouter, RouterHistoryMode } from '@wow-two-beta/ui-vue/router';
import { MarketingWorkspaceKey, type MarketingWorkspace } from '../src/application/marketing';
import type { DraftModel, WorkspaceModel } from '../src/domain/marketing';
import PostComposerPage from '../src/presentation/marketing/PostComposerPage.vue';

const wrappers: ReturnType<typeof mount>[] = [];
afterEach(() => {
  for (const wrapper of wrappers.splice(0)) wrapper.unmount();
});

/** Builds a deliberately small test fixture, never used by the product runtime. */
function fixture(): WorkspaceModel {
  return {
    brand: { name: 'Ocharo', timezone: 'Asia/Tashkent', handle: null },
    products: [
      {
        id: 'product',
        name: 'Nimcha',
        category: 'Nimcha',
        material: 'Synthetic',
        description: '',
        heroAssetId: 'front',
        assetIds: ['front'],
        sizes: ['S'],
        price: null,
        currency: 'UZS',
        reviewStatus: 'pending',
        sourceRevision: 'test',
        issues: [],
      },
    ],
    assets: [
      {
        id: 'front',
        fileName: 'front.png',
        contentType: 'image/png',
        byteLength: 1,
        sha256: 'test',
        provenance: 'test fixture',
        url: '/front.png',
        role: 'front',
      },
    ],
    presets: [
      {
        id: 't02-bone',
        name: 'Bone',
        family: 'T02',
        surface: 'bone',
        description: '',
        requiresFabric: false,
        width: 1080,
        height: 1350,
      },
    ],
    drafts: [],
    schedules: [],
    connections: [],
    metrics: [],
    generationJobs: [],
  };
}

/** Mounts the real composer with explicit application ports. */
async function render(original?: DraftModel, assetRole = 'front') {
  const source = fixture();
  source.assets[0]!.role = assetRole;
  if (original) source.drafts = [original];
  const workspace = shallowRef(source);
  const job = {
    id: 'job',
    kind: 'card',
    provider: 'local',
    prompt: '',
    productId: 'product',
    status: 'queued',
    createdAt: '2026-09-27T00:00:00Z',
    outputAssetIds: [] as string[],
    message: null,
  };
  const renderCard = vi.fn(async () => {
    workspace.value = { ...workspace.value, generationJobs: [job] };
    return ResultExtensions.ok(job);
  });
  const desk = {
    workspace,
    busy: ref(null),
    error: ref(null),
    notice: ref(null),
    navigate: vi.fn(),
    renderCard,
  } as unknown as MarketingWorkspace;
  const router = createAppRouter([{ path: '/', component: { template: '<div />' } }], {
    history: RouterHistoryMode.Memory,
    routePersistence: false,
  });
  await router.push('/');
  await router.isReady();
  const wrapper = mount(PostComposerPage, {
    props: { draftId: original?.id },
    global: { plugins: [router], provide: { [MarketingWorkspaceKey as symbol]: desk }, stubs: { CardPreview: true } },
  });
  wrappers.push(wrapper);
  return { wrapper, workspace, renderCard, desk };
}

describe('post composer output ownership', () => {
  it('keeps later edits unapproved when an earlier approval response arrives', async () => {
    const original: DraftModel = {
      id: 'saved',
      title: 'Nimcha',
      productId: 'product',
      presetId: 't02-bone',
      locale: 'uz',
      caption: 'Caption A',
      channels: ['instagram'],
      mediaIds: ['front'],
      revision: 1,
      approvedRevision: null,
      status: 'draft',
      createdAt: '2026-09-27T00:00:00Z',
      updatedAt: '2026-09-27T00:00:00Z',
    };
    const { wrapper, desk } = await render(original);
    let finish!: (result: Awaited<ReturnType<MarketingWorkspace['approveDraft']>>) => void;
    desk.approveDraft = vi.fn(
      () =>
        new Promise<Awaited<ReturnType<MarketingWorkspace['approveDraft']>>>((resolve) => {
          finish = resolve;
        }),
    );
    await wrapper
      .findAll('button')
      .find((button) => button.text().includes('Tekshirildi deb belgilash'))!
      .trigger('click');
    await flushPromises();
    await wrapper.get('textarea[aria-label="Post matni"]').setValue('Caption B');
    finish(ResultExtensions.ok({ ...original, status: 'approved', approvedRevision: 1 }));
    await flushPromises();
    expect((wrapper.get('textarea[aria-label="Post matni"]').element as HTMLTextAreaElement).value).toBe('Caption B');
    expect(wrapper.text()).toContain('Saqlanmagan');
    expect(
      wrapper
        .findAll('button')
        .find((button) => button.text() === 'Eksport')!
        .attributes('disabled'),
    ).toBeDefined();
  });

  it('preserves edits entered while a save response is pending', async () => {
    const { wrapper, desk } = await render();
    let finish!: (result: Awaited<ReturnType<MarketingWorkspace['saveDraft']>>) => void;
    desk.saveDraft = vi.fn(
      () =>
        new Promise<Awaited<ReturnType<MarketingWorkspace['saveDraft']>>>((resolve) => {
          finish = resolve;
        }),
    );
    await wrapper.get('textarea[aria-label="Post matni"]').setValue('Caption A');
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Saqlash')!
      .trigger('click');
    await flushPromises();
    await wrapper.get('textarea[aria-label="Post matni"]').setValue('Caption B');
    finish(
      ResultExtensions.ok({
        id: 'saved',
        title: 'Nimcha',
        productId: 'product',
        presetId: 't02-bone',
        locale: 'uz',
        caption: 'Caption A',
        channels: ['instagram'],
        mediaIds: [],
        revision: 1,
        approvedRevision: null,
        status: 'draft',
        createdAt: '2026-09-27T00:00:00Z',
        updatedAt: '2026-09-27T00:00:00Z',
      }),
    );
    await flushPromises();
    expect((wrapper.get('textarea[aria-label="Post matni"]').element as HTMLTextAreaElement).value).toBe('Caption B');
    expect(wrapper.text()).toContain('Saqlanmagan');
  });

  it('reopens the saved recipe while retaining its explicit selected output', async () => {
    const { wrapper } = await render({
      id: 'saved',
      title: 'Nimcha',
      productId: 'product',
      presetId: 't02-bone',
      locale: 'uz',
      caption: 'Caption',
      channels: ['instagram'],
      mediaIds: ['front'],
      revision: 3,
      approvedRevision: 3,
      status: 'approved',
      createdAt: '2026-09-27T00:00:00Z',
      updatedAt: '2026-09-27T00:00:00Z',
      composition: {
        title: 'Artwork title',
        subtitle: 'Saved subtitle',
        fabricAssetId: 'fabric',
        scale: 0.9,
        offsetX: 0.02,
        offsetY: -0.03,
      },
    });
    expect((wrapper.get('input[aria-label="Post sarlavhasi"]').element as HTMLInputElement).value).toBe(
      'Artwork title',
    );
    expect((wrapper.get('input[aria-label="Qo‘shimcha satr"]').element as HTMLInputElement).value).toBe(
      'Saved subtitle',
    );
    expect((wrapper.get('input[aria-label="Masshtab"]').element as HTMLInputElement).value).toBe('0.9');
    expect((wrapper.get('input[aria-label="Gorizontal siljish"]').element as HTMLInputElement).value).toBe('0.02');
    expect(wrapper.find('img[alt="Postga qo‘shilgan tayyor tasvir"]').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('Saqlanmagan');
  });

  it('offers a finished image only while the rendered composition remains unchanged', async () => {
    const { wrapper, workspace } = await render();
    await wrapper
      .findAll('button')
      .find((button) => button.text().includes('Tasvirni tayyorlash'))!
      .trigger('click');
    await flushPromises();
    workspace.value = {
      ...workspace.value,
      generationJobs: workspace.value.generationJobs.map((job) => ({
        ...job,
        status: 'completed',
        outputAssetIds: ['output'],
      })),
      assets: [...workspace.value.assets, { ...workspace.value.assets[0]!, id: 'output', url: '/output.png' }],
    };
    await flushPromises();
    expect(wrapper.findAll('button').some((button) => button.text() === 'Postga qo‘shish')).toBe(true);
    await wrapper.get('input[aria-label="Post sarlavhasi"]').setValue('Changed title');
    expect(wrapper.findAll('button').some((button) => button.text() === 'Postga qo‘shish')).toBe(false);
    expect(wrapper.text()).toContain('tasvirni qayta tayyorlang');
  });

  it('preserves selected original photographs while editing the title and caption', async () => {
    const { wrapper, desk } = await render();
    desk.saveDraft = vi.fn(async () => ResultExtensions.fail(ApiFailureFactory.create('transport')));
    await wrapper.get('button[aria-label="front.png"]').trigger('click');
    await wrapper.get('input[aria-label="Post sarlavhasi"]').setValue('Independent photograph');
    await wrapper.get('textarea[aria-label="Post matni"]').setValue('An edited caption');
    expect(wrapper.get('button[aria-label="front.png"]').attributes('aria-pressed')).toBe('true');
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Saqlash')!
      .trigger('click');
    await flushPromises();
    expect(desk.saveDraft).toHaveBeenCalledWith(
      expect.objectContaining({ mediaIds: ['front'], caption: 'An edited caption' }),
      undefined,
    );
  });

  it('does not infer recipe ownership for a saved independently selected library card', async () => {
    const { wrapper } = await render(
      {
        id: 'saved-library',
        title: 'Saved title',
        productId: 'product',
        presetId: 't02-bone',
        locale: 'uz',
        caption: 'Caption',
        channels: ['instagram'],
        mediaIds: ['front'],
        revision: 1,
        approvedRevision: null,
        status: 'draft',
        createdAt: '2026-09-27T00:00:00Z',
        updatedAt: '2026-09-27T00:00:00Z',
        composition: {
          title: 'Saved title',
          subtitle: '',
          fabricAssetId: null,
          scale: 1,
          offsetX: 0,
          offsetY: 0,
          boundMediaIds: [],
        },
      },
      'card',
    );
    await wrapper.get('input[aria-label="Post sarlavhasi"]').setValue('New title');
    expect(wrapper.get('button[aria-label="front.png"]').attributes('aria-pressed')).toBe('true');
  });

  it('invalidates selected artwork after a composition change', async () => {
    const { wrapper, workspace } = await render();
    await wrapper
      .findAll('button')
      .find((button) => button.text().includes('Tasvirni tayyorlash'))!
      .trigger('click');
    await flushPromises();
    workspace.value = {
      ...workspace.value,
      generationJobs: workspace.value.generationJobs.map((job) => ({
        ...job,
        status: 'completed',
        outputAssetIds: ['output'],
      })),
      assets: [...workspace.value.assets, { ...workspace.value.assets[0]!, id: 'output', url: '/output.png' }],
    };
    await flushPromises();
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Postga qo‘shish')!
      .trigger('click');
    expect(wrapper.find('img[alt="Postga qo‘shilgan tayyor tasvir"]').exists()).toBe(true);
    await wrapper.get('input[aria-label="Qo‘shimcha satr"]').setValue('New line');
    expect(wrapper.find('img[alt="Postga qo‘shilgan tayyor tasvir"]').exists()).toBe(false);
    expect(wrapper.text()).toContain('Saqlanmagan');
  });
});
