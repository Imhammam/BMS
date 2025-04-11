using BMS1.Models;
using Microsoft.EntityFrameworkCore;

namespace BMS1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Tender> Tenders { get; set; }
        public DbSet<Bid> Bids { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // العلاقة بين Bid و Tender
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Tender)
                .WithMany()
                .HasForeignKey(b => b.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            // العلاقة بين Tender و WinningBid
            modelBuilder.Entity<Tender>()
                .HasOne(t => t.WinningBid)
                .WithMany()
                .HasForeignKey(t => t.WinningBidId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
