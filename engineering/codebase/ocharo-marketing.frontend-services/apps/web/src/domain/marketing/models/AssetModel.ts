/** Represents asset data used by the marketing workspace. */
export interface AssetModel {
  /** The id value. */
  id: string;

  /** The fileName value. */
  fileName: string;

  /** The contentType value. */
  contentType: string;

  /** The byteLength value. */
  byteLength: number;

  /** The sha256 value. */
  sha256: string;

  /** The provenance value. */
  provenance: string;

  /** The url value. */
  url: string;

  /** The role value. */
  role: string;
}
