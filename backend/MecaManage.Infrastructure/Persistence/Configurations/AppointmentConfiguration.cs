using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.SpecialRequests).HasMaxLength(500);
        builder.Property(a => a.DeclineReason).HasMaxLength(500);

        builder.HasOne(a => a.Client)
               .WithMany(u => u.ClientAppointments)
               .HasForeignKey(a => a.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Vehicle)
               .WithMany(v => v.Appointments)
               .HasForeignKey(a => a.VehicleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Garage)
               .WithMany(g => g.Appointments)
               .HasForeignKey(a => a.GarageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SymptomReport)
               .WithMany(s => s.Appointments)
               .HasForeignKey(a => a.SymptomReportId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne(a => a.ApprovedByChef)
               .WithMany(u => u.ApprovedAppointments)
               .HasForeignKey(a => a.ApprovedByChefId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne(a => a.Invoice)
               .WithOne(i => i.Appointment)
               .HasForeignKey<Invoice>(i => i.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.RepairTask)
               .WithOne(t => t.Appointment)
               .HasForeignKey<RepairTask>(t => t.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

