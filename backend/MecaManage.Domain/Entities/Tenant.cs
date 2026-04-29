using MecaManage.Domain.Common;

namespace MecaManage.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Garage> Garages { get; set; } = new List<Garage>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();
}