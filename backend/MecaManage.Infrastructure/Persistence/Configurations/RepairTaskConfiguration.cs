using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class RepairTaskConfiguration : IEntityTypeConfiguration<RepairTask>
{
    public void Configure(EntityTypeBuilder<RepairTask> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TaskTitle).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(2000);
        builder.Property(t => t.CompletionNotes).HasMaxLength(2000);

        builder.HasOne(t => t.Appointment)
               .WithOne(a => a.RepairTask)
               .HasForeignKey<RepairTask>(t => t.AppointmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Garage)
               .WithMany(g => g.RepairTasks)
               .HasForeignKey(t => t.GarageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Tenant)
               .WithMany(ten => ten.RepairTasks)
               .HasForeignKey(t => t.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedByChef)
               .WithMany(u => u.AssignedRepairTasks)
               .HasForeignKey(t => t.AssignedByChefId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Assignments)
               .WithOne(a => a.RepairTask)
               .HasForeignKey(a => a.RepairTaskId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RepairTaskAssignmentConfiguration : IEntityTypeConfiguration<RepairTaskAssignment>
{
    public void Configure(EntityTypeBuilder<RepairTaskAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.MechanicNotes).HasMaxLength(2000);

        builder.HasOne(a => a.RepairTask)
               .WithMany(t => t.Assignments)
               .HasForeignKey(a => a.RepairTaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Mechanic)
               .WithMany(u => u.MechanicAssignments)
               .HasForeignKey(a => a.MechanicId)
               .OnDelete(DeleteBehavior.Restrict);

        // Create unique index to prevent duplicate assignments
        builder.HasIndex(a => new { a.RepairTaskId, a.MechanicId }).IsUnique();
    }
}

