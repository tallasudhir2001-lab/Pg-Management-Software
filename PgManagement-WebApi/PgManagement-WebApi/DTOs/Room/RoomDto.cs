namespace PgManagement_WebApi.DTOs.Room
{
    public class RoomDto
    {
        public string RoomId { get; set; }
        public string RoomNumber { get; set; }

        public int Capacity { get; set; }
        public int Occupied { get; set; }
        public int Vacancies { get; set; }

        public decimal RentAmount { get; set; }

        public string Status { get; set; } // Available | Partial | Full

        public bool isAc {  get; set; }
    }
}
