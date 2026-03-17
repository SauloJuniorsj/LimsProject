namespace LimsProject.Models
{
    public class LabAnalysis
    {
        public Guid Id { get; set; }
        public Guid BatchId { get; set; }
        public decimal THC { get; set; } // Teor de THC
        public decimal CBD { get; set; } // Teor de CBD
        public string Terpenes { get; set; } = string.Empty; // Aromas/Terpenos
        public DateTime AnalysisDate { get; set; }
        public bool IsPassed { get; set; } // Se passou no teste de qualidade
    }
}
