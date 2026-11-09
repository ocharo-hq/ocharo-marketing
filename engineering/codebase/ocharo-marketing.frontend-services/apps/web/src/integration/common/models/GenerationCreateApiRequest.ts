/** Defines the GenerationCreateApiRequest wire payload. */
export interface GenerationCreateApiRequest {
  /** The provider value. */
  provider: string;

  /** The prompt value. */
  prompt: string;

  /** The productId value. */
  productId?: string;

  /** The referenceAssetIds value. */
  referenceAssetIds?: string[];

  /** The model value. */
  model?: string;
}
