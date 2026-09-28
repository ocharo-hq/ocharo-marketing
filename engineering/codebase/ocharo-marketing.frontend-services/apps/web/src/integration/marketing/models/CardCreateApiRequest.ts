/** Defines the CardCreateApiRequest wire payload. */
export interface CardCreateApiRequest {
  /** The productId value. */
  productId: string;

  /** The presetId value. */
  presetId: string;

  /** The title value. */
  title: string;

  /** The subtitle value. */
  subtitle?: string;

  /** The locale value. */
  locale: string;

  /** The fabricAssetId value. */
  fabricAssetId?: string;

  /** The scale value. */
  scale?: number;

  /** The offsetX value. */
  offsetX?: number;

  /** The offsetY value. */
  offsetY?: number;
}
