using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Room
{
    public class UpdateRoomDto
    {
        [Required]
        public string RoomNumber { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal RentAmount { get; set; }
        public bool isAc { get; set; }
    }
}
