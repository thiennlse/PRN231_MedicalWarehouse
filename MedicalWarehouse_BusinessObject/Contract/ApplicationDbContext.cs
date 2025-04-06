using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MedicalWarehouse_BusinessObject.Contract
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Medical> Medicals { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ShipmentDetail> ShipmentDetails { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Area> Area { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.HasMany(u => u.Orders)
                      .WithOne(o => o.User)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Shipment>(entity =>
            {
                entity.HasOne(s => s.Area)
                      .WithMany(a => a.Shipment)
                      .HasForeignKey(s => s.AreaId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(s => s.ShipmentDetails)
                      .WithOne(sd => sd.Shipment)
                      .HasForeignKey(sd => sd.ShipmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Medical>(entity =>
            {
                entity.HasMany(m => m.ShipmentDetails)
                      .WithOne(sd => sd.Medical)
                      .HasForeignKey(sd => sd.MedicalId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Supplier>(entity =>
            {
                entity.HasMany(s => s.Shipments)
                      .WithOne(sm => sm.Supplier)
                      .HasForeignKey(sm => sm.SupplierId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Order>(entity =>
            {

                entity.HasMany(o => o.OrderDetails)
                      .WithOne(od => od.Orders)
                      .HasForeignKey(od => od.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(o => o.Type)
                      .HasConversion(new EnumToStringConverter<OrderType>());

                entity.Property(o => o.Status)
                      .HasConversion(new EnumToStringConverter<OrderStatus>());
            });

        }
    }
}
