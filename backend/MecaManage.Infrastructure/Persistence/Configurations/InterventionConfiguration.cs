using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class InterventionConfiguration : IEntityTypeConfiguration<Intervention>
{
    public void Configure(EntityTypeBuilder<Intervention> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Status)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(i => i.PaymentMethod).HasMaxLength(100);
        builder.Property(i => i.PaidBy).HasMaxLength(200);
        builder.Property(i => i.ExaminationNotes).HasMaxLength(4000);
        builder.Property(i => i.RepairNotes).HasMaxLength(4000);
        builder.Property(i => i.PaymentAmount).HasPrecision(18, 2);

        // Required FKs — restrict delete so the tracker stays for history
        builder.HasOne(i => i.Tenant)
               .WithMany()
               .HasForeignKey(i => i.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Garage)
               .WithMany()
               .HasForeignKey(i => i.GarageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Client)
               .WithMany()
               .HasForeignKey(i => i.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Vehicle)
               .WithMany()
               .HasForeignKey(i => i.VehicleId)
               .OnDelete(DeleteBehavior.Restrict);

        // Optional links — set null on delete so history is not lost
        builder.HasOne(i => i.Appointment)
               .WithMany()
               .HasForeignKey(i => i.AppointmentId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne(i => i.SymptomReport)
               .WithMany()
               .HasForeignKey(i => i.SymptomReportId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne(i => i.Invoice)
               .WithMany()
               .HasForeignKey(i => i.InvoiceId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}

