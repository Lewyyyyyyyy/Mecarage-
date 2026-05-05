using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Garage> Garages { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<InterventionRequest> InterventionRequests { get; }
    DbSet<AIDiagnosis> AIDiagnoses { get; }

    // New Workshop Lifecycle Entities
    DbSet<SymptomReport> SymptomReports { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<SparePart> SpareParts { get; }
    DbSet<RepairTask> RepairTasks { get; }
    DbSet<RepairTaskAssignment> RepairTaskAssignments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Intervention> Interventions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}