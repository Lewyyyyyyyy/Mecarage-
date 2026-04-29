using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.ServiceFee).HasPrecision(18, 2);
        builder.Property(i => i.PartsTotal).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);

        builder.HasOne(i => i.Appointment)
               .WithOne(a => a.Invoice)
               .HasForeignKey<Invoice>(i => i.AppointmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Tenant)
               .WithMany(t => t.Invoices)
               .HasForeignKey(i => i.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Client)
               .WithMany(u => u.CreatedInvoices)
               .HasForeignKey(i => i.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Garage)
               .WithMany()
               .HasForeignKey(i => i.GarageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.LineItems)
               .WithOne(li => li.Invoice)
               .HasForeignKey(li => li.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        // Create index for invoice number
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.HasKey(li => li.Id);
        builder.Property(li => li.Description).IsRequired().HasMaxLength(500);
        builder.Property(li => li.UnitPrice).HasPrecision(18, 2);
        builder.Property(li => li.LineTotal).HasPrecision(18, 2);

        builder.HasOne(li => li.Invoice)
               .WithMany(i => i.LineItems)
               .HasForeignKey(li => li.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(li => li.SparePart)
               .WithMany(sp => sp.InvoiceLineItems)
               .HasForeignKey(li => li.SparePartId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}

