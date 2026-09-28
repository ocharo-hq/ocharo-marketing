/** Represents generationjob data used by the marketing workspace. */
export interface GenerationJobModel {
  /** The id value. */
  id: string;

  /** The kind value. */
  kind: string;

  /** The provider value. */
  provider: string;

  /** The prompt value. */
  prompt: string;

  /** The productId value. */
  productId: string | null;

  /** The status value. */
  status: string;

  /** The createdAt value. */
  createdAt: string;

  /** The outputAssetIds value. */
  outputAssetIds: string[];

  /** The message value. */
  message: string | null;
}
