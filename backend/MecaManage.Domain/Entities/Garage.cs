using MecaManage.Domain.Common;

namespace MecaManage.Domain.Entities;

public class Garage : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? AdminId { get; set; }  // The garage manager admin (ChefAtelier)

    public Tenant Tenant { get; set; } = null!;
    public User? Admin { get; set; }  // Navigation property to the admin user
    public ICollection<User> Staff { get; set; } = new List<User>();
    public ICollection<InterventionRequest> Interventions { get; set; } = new List<InterventionRequest>();
    public ICollection<SparePart> SpareParts { get; set; } = new List<SparePart>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();
}