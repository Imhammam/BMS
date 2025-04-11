namespace BMS1.Models
{
    public class Bid
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenderId { get; set; }
        public Tender? Tender { get; set; }

        public int BidderId { get; set; }  
        public User? Bidder { get; set; }

        public string ProposalSummary { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Submitted"; 
    }
}