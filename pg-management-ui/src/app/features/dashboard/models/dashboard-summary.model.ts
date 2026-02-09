export interface DashboardSummary {
  totalRooms: number;
  totalTenants: number;

  activeTenants: number;
  movedOutTenants: number;

  occupiedBeds: number;
  vacantBeds: number;

  monthlyRevenue: number;
}
