/** Represents the bounded artwork settings reviewed by the operator. */
export interface CardCompositionModel {
  /** The imported product identity. */
  productId: string;

  /** The named composition preset. */
  presetId: string;

  /** The displayed headline. */
  title: string;

  /** The optional secondary line. */
  subtitle?: string;

  /** The artwork language. */
  locale: string;

  /** The matching fabric photograph identity. */
  fabricAssetId?: string;

  /** The bounded garment scale. */
  scale?: number;

  /** The horizontal fraction of the card. */
  offsetX?: number;

  /** The vertical fraction of the card. */
  offsetY?: number;
}
