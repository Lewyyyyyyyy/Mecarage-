using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class GarageConfiguration : IEntityTypeConfiguration<Garage>
{
    public void Configure(EntityTypeBuilder<Garage> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Address).IsRequired().HasMaxLength(500);
        builder.Property(g => g.City).IsRequired().HasMaxLength(100);

        builder.HasOne(g => g.Tenant)
               .WithMany(t => t.Garages)
               .HasForeignKey(g => g.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        // Configure the optional one-to-one relationship with Admin (User)
        builder.HasOne(g => g.Admin)
               .WithOne()
               .HasForeignKey<Garage>(g => g.AdminId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}