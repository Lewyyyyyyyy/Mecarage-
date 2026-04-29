using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class InterventionRequestConfiguration : IEntityTypeConfiguration<InterventionRequest>
{
    public void Configure(EntityTypeBuilder<InterventionRequest> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Status).HasConversion<string>();
        builder.Property(i => i.UrgencyLevel).HasConversion<string>();

        builder.HasOne(i => i.Tenant)
               .WithMany()
               .HasForeignKey(i => i.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Client)
               .WithMany(u => u.InterventionRequests)
               .HasForeignKey(i => i.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Vehicle)
               .WithMany(v => v.Interventions)
               .HasForeignKey(i => i.VehicleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Garage)
               .WithMany(g => g.Interventions)
               .HasForeignKey(i => i.GarageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AssignedMecanicien)
               .WithMany()
               .HasForeignKey(i => i.AssignedMecanicienId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.AIDiagnosis)
               .WithOne(a => a.InterventionRequest)
               .HasForeignKey<AIDiagnosis>(a => a.InterventionRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}