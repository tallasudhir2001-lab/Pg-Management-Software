export interface Room {
  roomId: string;
  roomNumber: string;

  capacity: number;
  occupied: number;
  vacancies: number;

  rentAmount: number;

  status: 'Available' | 'Partial' | 'Full';
    isAc: boolean; 
}