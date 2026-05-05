using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Garage> Garages => Set<Garage>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<InterventionRequest> InterventionRequests => Set<InterventionRequest>();
    public DbSet<AIDiagnosis> AIDiagnoses => Set<AIDiagnosis>();

    // New Workshop Lifecycle Entities
    public DbSet<SymptomReport> SymptomReports => Set<SymptomReport>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<RepairTask> RepairTasks => Set<RepairTask>();
    public DbSet<RepairTaskAssignment> RepairTaskAssignments => Set<RepairTaskAssignment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Intervention> Interventions => Set<Intervention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Garage>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Vehicle>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<InterventionRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AIDiagnosis>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SymptomReport>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<InvoiceLineItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SparePart>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RepairTask>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RepairTaskAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Intervention>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                if (entry.State == EntityState.Modified)
                    entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}