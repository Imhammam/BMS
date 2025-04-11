namespace BMS1.Models.DTO
{
    public class TenderDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public DateTime Deadline { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; }
        public string? Location { get; set; }
    }
}