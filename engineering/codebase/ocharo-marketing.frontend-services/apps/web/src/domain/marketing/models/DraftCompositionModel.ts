/** Records the operator-reviewed recipe used to create an attached card. */
export interface DraftCompositionModel {
  /** Explicit outputs rendered from this recipe; an arbitrary library card is independent. */
  boundMediaIds?: string[] | null;
  /** The card heading. */
  title: string;
  /** The secondary card line. */
  subtitle: string;
  /** The explicitly chosen matching fabric source. */
  fabricAssetId: string | null;
  /** The bounded product scale. */
  scale: number;
  /** The horizontal fraction of the card width. */
  offsetX: number;
  /** The vertical fraction of the card height. */
  offsetY: number;
}
