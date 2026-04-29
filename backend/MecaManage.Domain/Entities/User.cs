using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? TenantId { get; set; }  // Tenant this user belongs to
    public Guid? GarageId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public Tenant? Tenant { get; set; }
    public Garage? Garage { get; set; }
    public ICollection<InterventionRequest> InterventionRequests { get; set; } = new List<InterventionRequest>();
    public ICollection<SymptomReport> SubmittedSymptomReports { get; set; } = new List<SymptomReport>();
    public ICollection<SymptomReport> ReviewedSymptomReports { get; set; } = new List<SymptomReport>();
    public ICollection<Appointment> ClientAppointments { get; set; } = new List<Appointment>();
    public ICollection<Appointment> ApprovedAppointments { get; set; } = new List<Appointment>();
    public ICollection<Invoice> CreatedInvoices { get; set; } = new List<Invoice>();
    public ICollection<RepairTask> AssignedRepairTasks { get; set; } = new List<RepairTask>();
    public ICollection<RepairTaskAssignment> MechanicAssignments { get; set; } = new List<RepairTaskAssignment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}