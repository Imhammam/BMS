using System.Security.Cryptography;

namespace BMS1.Models
{
    public class Tender
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public DateTime Deadline { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = "Open";
        public string Location { get; set; } = string.Empty;
        public string CreatedByEmail { get; set; } = string.Empty;

        public Guid? WinningBidId { get; set; }
        public Bid? WinningBid { get; set; }
    }
}
