using System.ComponentModel.DataAnnotations;

namespace LimsProject.Models
{
    public class SensorData
    {
        public Guid Id { get; set; }
        public Guid BatchId { get; set; }
        public decimal Temperature { get; set; }
        public DateTime ReadingTime { get; set; }
    }
}
