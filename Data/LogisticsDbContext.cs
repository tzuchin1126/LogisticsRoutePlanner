using LogisticsRoutePlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsRoutePlanner.Data
{
    public class LogisticsDbContext : DbContext
    {
        public LogisticsDbContext(DbContextOptions<LogisticsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentDestination> ShipmentDestinations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置 Shipment 和 ShipmentDestination 之間的關係
            modelBuilder.Entity<Shipment>()
                .HasMany(s => s.Destinations)
                .WithOne(d => d.Shipment)
                .HasForeignKey(d => d.ShipmentId);
        }
    }
}
