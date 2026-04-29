using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class SymptomReportConfiguration : IEntityTypeConfiguration<SymptomReport>
{
    public void Configure(EntityTypeBuilder<SymptomReport> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SymptomsDescription).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.AIPredictedIssue).HasMaxLength(2000);
        builder.Property(s => s.AIRecommendations).HasMaxLength(2000);
        builder.Property(s => s.ChefFeedback).HasMaxLength(2000);

        builder.HasOne(s => s.Client)
               .WithMany(u => u.SubmittedSymptomReports)
               .HasForeignKey(s => s.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Vehicle)
               .WithMany(v => v.SymptomReports)
               .HasForeignKey(s => s.VehicleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Garage)
               .WithMany()
               .HasForeignKey(s => s.GarageId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne(s => s.ReviewedByChef)
               .WithMany(u => u.ReviewedSymptomReports)
               .HasForeignKey(s => s.ReviewedByChefId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasMany(s => s.Appointments)
               .WithOne(a => a.SymptomReport)
               .HasForeignKey(a => a.SymptomReportId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

