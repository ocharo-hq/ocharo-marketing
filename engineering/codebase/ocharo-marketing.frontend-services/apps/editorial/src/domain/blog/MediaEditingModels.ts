/** Processing limits returned by the current Blog host. */
export interface MediaEditingCapabilities {
  crop: boolean;
  rotate: boolean;
  flip: boolean;
  resize: boolean;
  formats: ('png' | 'jpeg' | 'webp')[];
  maximumBytes: number;
  maximumPixels: number;
  maximumDimension: number;
  backgroundRemoval: { available: boolean; model: string | null };
}

/** An ephemeral processed image; previewing never stores an asset. */
export interface MediaEditPreview {
  blob: Blob;
  width: number;
  height: number;
}
