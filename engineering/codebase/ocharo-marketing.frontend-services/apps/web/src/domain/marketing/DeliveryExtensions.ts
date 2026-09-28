import type { WorkspaceModel } from './models/WorkspaceModel';
import type { ScheduleModel } from './models/ScheduleModel';
/** Explains why delivery is unavailable using current recorded evidence. */
export const DeliveryExtensions = {
  blockers(workspace: WorkspaceModel | null, plan: ScheduleModel, now: number = Date.now()): string[] {
    if (!workspace) return ['Ish stoli yuklanmagan.'];
    const reasons: string[] = [];
    const draft = workspace.drafts.find((item) => item.id === plan.draftId);
    const connection = workspace.connections.find((item) => item.id === plan.channel && item.kind === 'social');
    if (!workspace.deliverySettings?.publishingEnabled)
      reasons.push('Avtomatik joylash hozir o‘chirilgan. Qo‘lda joylash rejasi saqlanadi.');
    if (plan.status !== 'planned') reasons.push('Faqat hali yuborilmagan reja navbatga qo‘shiladi.');
    if (workspace.publicationOperations?.some((item) => item.scheduleId === plan.id))
      reasons.push('Bu rejada yuborish tarixi mavjud. Yangi niyat uchun yangi reja yarating.');
    if (!draft || draft.revision !== plan.draftRevision || draft.approvedRevision !== plan.draftRevision)
      reasons.push('Rejadagi talqin hozirgi tekshirilgan post bilan mos emas. Yangi reja yarating.');
    if (
      !connection ||
      connection.verificationStatus !== 'verified' ||
      !connection.accountId ||
      !connection.destinationId ||
      !connection.verifiedAt
    )
      reasons.push('Kanal akkaunti va manzil hali tekshirilmagan.');
    if (!connection?.executionEnabled) reasons.push('Bu kanalga yuborish yoqilmagan.');
    const capability = draft?.mediaIds.length ? 'photo' : 'text';
    if (!connection?.capabilities.includes(capability)) reasons.push('Bu kanal tanlangan post formatini qo‘llamaydi.');
    if (draft?.mediaIds.some((id) => !workspace.assets.some((asset) => asset.id === id && asset.sha256)))
      reasons.push('Post tasvirining asl manbasi topilmadi.');
    if (!Number.isFinite(Date.parse(plan.scheduledAt)) || Date.parse(plan.scheduledAt) <= now)
      reasons.push('Reja vaqti kelajakda bo‘lishi kerak.');
    if (!['telegram', 'x'].includes(plan.channel))
      reasons.push('Bu kanalda avtomatik yuborish yo‘li hali mavjud emas.');
    if (draft) {
      if (draft.mediaIds.length > 1) reasons.push('Bu yuborish yo‘li faqat bitta tasvirni qo‘llaydi.');
      const max = plan.channel === 'x' ? 280 : draft.mediaIds.length ? 1024 : 4096;
      if (!draft.caption.trim() || Array.from(draft.caption).length > max)
        reasons.push(`Post matni bo‘sh bo‘lmasin va ${max} belgidan oshmasin. Matn qisqartirilmaydi.`);
      for (const id of draft.mediaIds) {
        const asset = workspace.assets.find((item) => item.id === id);
        if (asset && !['image/png', 'image/jpeg'].includes(asset.contentType))
          reasons.push('Yuborish tasviri PNG yoki JPEG bo‘lishi kerak.');
        if (asset && asset.byteLength > (plan.channel === 'x' ? 5242880 : 10485760))
          reasons.push('Tasvir bu kanalning hajm chegarasidan katta.');
      }
    }
    return reasons;
  },
  reviewKey(workspace: WorkspaceModel | null, plan: ScheduleModel): string | null {
    const draft = workspace?.drafts.find((item) => item.id === plan.draftId);
    const connection = workspace?.connections.find((item) => item.id === plan.channel);
    if (!draft || !connection) return null;
    return JSON.stringify({
      plan,
      draft,
      connection,
      assets: draft.mediaIds.map((id) => workspace?.assets.find((item) => item.id === id)),
      settings: workspace?.deliverySettings,
    });
  },
};
