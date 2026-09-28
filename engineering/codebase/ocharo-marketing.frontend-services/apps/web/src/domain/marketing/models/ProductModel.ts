/** Represents product data used by the marketing workspace. */
export interface ProductModel {
  /** The id value. */
  id: string;

  /** The name value. */
  name: string;

  /** The category value. */
  category: string;

  /** The material value. */
  material: string;

  /** The description value. */
  description: string;

  /** The heroAssetId value. */
  heroAssetId: string;

  /** The assetIds value. */
  assetIds: string[];

  /** The sizes value. */
  sizes: string[];

  /** The price value. */
  price: number | null;

  /** The currency value. */
  currency: string;

  /** The reviewStatus value. */
  reviewStatus: string;

  /** The sourceRevision value. */
  sourceRevision: string;

  /** The issues value. */
  issues: string[];

  /** Localized copy imported from Ocharo Catalogue (uz/ru/en), when synced. */
  copy?: Record<string, ProductCopyModel> | null;

  /** Lowest (bulk) unit price from Ocharo Catalogue, when synced. */
  wholesalePrice?: number | null;
}

/** One language's product copy from Ocharo Catalogue. */
export interface ProductCopyModel {
  status: string;
  name: string | null;
  fabric: string | null;
  decoration: string | null;
  description: string | null;
}
