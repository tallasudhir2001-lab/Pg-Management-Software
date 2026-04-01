export interface CollectionSummary {
  expectedRent: number;
  collectedRent: number;
  pendingRent: number;
  collectionRate: number;   // 0–100
  paidCount: number;
  pendingCount: number;
}
