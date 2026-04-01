export interface AuditEvent {
  id: string;
  eventType: string;
  entityType: string;
  entityId: string;
  description: string | null;
  oldValue: string | null;
  newValue: string | null;
  performedBy: string;
  performedAt: string;
  isReviewed: boolean;
  reviewedBy: string | null;
  reviewedAt: string | null;
}

export interface AuditCount {
  unreviewedCount: number;
}
