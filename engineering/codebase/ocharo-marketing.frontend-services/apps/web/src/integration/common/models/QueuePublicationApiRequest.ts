/** Binds explicit consent to reviewed draft and destination revisions. */
export interface QueuePublicationApiRequest {
  expectedDraftRevision: number;
  connectionId: string;
  expectedConnectionRevision: number;
  operationId: string;
}
