import { afterEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/ui-vue/foundation/http';
import { createMarketingWorkspace } from '../src/application/marketing/UseMarketingWorkspace';
import { marketingApi } from '../src/integration/marketing';
import type { WorkspaceModel, ScheduleModel } from '../src/domain/marketing';
function deferred<T>() { let resolve!: (value:T)=>void;const promise=new Promise<T>(settle=>{resolve=settle;});return {promise,resolve}; }
const projection=(title:string)=>({drafts:[{id:'draft',title}],generationJobs:[],schedules:[]} as unknown as WorkspaceModel);
afterEach(()=>vi.restoreAllMocks());
describe('marketing acknowledged writes and read projections',()=>{
 it('keeps the workspace unchanged before acknowledgement, skeletons only the follow-up read, and fills from GET',async()=>{
  const desk=createMarketingWorkspace(vi.fn());const original=projection('Before'),current=projection('Independent GET');
  const get=vi.spyOn(marketingApi,'getWorkspace').mockResolvedValueOnce(ResultExtensions.ok(original));await desk.refresh();
  const write=deferred<ReturnType<typeof ResultExtensions.ok<ScheduleModel>>>(),read=deferred<ReturnType<typeof ResultExtensions.ok<WorkspaceModel>>>();
  vi.spyOn(marketingApi,'cancelSchedule').mockReturnValue(write.promise);get.mockReturnValueOnce(read.promise);
  const pending=desk.cancelSchedule('plan');expect(desk.loading.value).toBe(false);expect(desk.workspace.value).toEqual(original);expect(get).toHaveBeenCalledTimes(1);
  write.resolve(ResultExtensions.ok({id:'plan',draftTitle:'Mutation payload'} as ScheduleModel));await Promise.resolve();await Promise.resolve();
  expect(get).toHaveBeenCalledTimes(2);expect(desk.loading.value).toBe(true);expect(desk.workspace.value).toEqual(original);
  read.resolve(ResultExtensions.ok(current));const receipt=await pending;expect(receipt.ok).toBe(true);expect(desk.workspace.value).toEqual(current);expect(desk.loading.value).toBe(false);
 });
 it('preserves the original workspace and confirmed receipt when the follow-up GET rejects',async()=>{
  const desk=createMarketingWorkspace(vi.fn()),original=projection('Before');const get=vi.spyOn(marketingApi,'getWorkspace').mockResolvedValueOnce(ResultExtensions.ok(original));await desk.refresh();
  vi.spyOn(marketingApi,'cancelSchedule').mockResolvedValue(ResultExtensions.ok({id:'plan'} as ScheduleModel));get.mockRejectedValueOnce(Error('private transport failure'));
  const receipt=await desk.cancelSchedule('plan');expect(receipt.ok).toBe(true);expect(desk.workspace.value).toEqual(original);expect(desk.loading.value).toBe(false);expect(desk.busy.value).toBeNull();expect(desk.error.value).toContain('Saved successfully');expect(desk.error.value).not.toContain('private');
 });
 it('does not refresh after a rejected mutation and keeps the current grid',async()=>{
  const desk=createMarketingWorkspace(vi.fn()),original=projection('Before');const get=vi.spyOn(marketingApi,'getWorkspace').mockResolvedValue(ResultExtensions.ok(original));await desk.refresh();
  vi.spyOn(marketingApi,'cancelSchedule').mockResolvedValue(ResultExtensions.fail(ApiFailureFactory.create('transport')));const receipt=await desk.cancelSchedule('plan');expect(receipt.ok).toBe(false);expect(get).toHaveBeenCalledTimes(1);expect(desk.workspace.value).toEqual(original);expect(desk.loading.value).toBe(false);
 });
});
