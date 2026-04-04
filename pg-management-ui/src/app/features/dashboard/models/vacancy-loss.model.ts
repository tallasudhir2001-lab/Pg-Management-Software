export interface VacancyLoss {
  totalMonthlyLoss: number;
  totalVacantBeds: number;
  totalBeds: number;
  rooms: VacancyLossRoom[];
}

export interface VacancyLossRoom {
  roomId: string;
  roomNumber: string;
  capacity: number;
  occupied: number;
  vacantBeds: number;
  rentPerBed: number;
  monthlyLoss: number;
}
