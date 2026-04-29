using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class SparePartConfiguration : IEntityTypeConfiguration<SparePart>
{
    public void Configure(EntityTypeBuilder<SparePart> builder)
    {
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Code).IsRequired().HasMaxLength(50);
        builder.Property(sp => sp.Name).IsRequired().HasMaxLength(200);
        builder.Property(sp => sp.Description).HasMaxLength(1000);
        builder.Property(sp => sp.Category).IsRequired().HasMaxLength(100);
        builder.Property(sp => sp.UnitPrice).HasPrecision(18, 2);
        builder.Property(sp => sp.Manufacturer).HasMaxLength(200);
        builder.Property(sp => sp.PartNumber).HasMaxLength(100);

        builder.HasOne(sp => sp.Garage)
               .WithMany(g => g.SpareParts)
               .HasForeignKey(sp => sp.GarageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sp => sp.InvoiceLineItems)
               .WithOne(li => li.SparePart)
               .HasForeignKey(li => li.SparePartId)
               .OnDelete(DeleteBehavior.SetNull);

        // Create index for code and garage
        builder.HasIndex(sp => new { sp.GarageId, sp.Code }).IsUnique();
    }
}

