using MecaManage.Domain.Common;

namespace MecaManage.Domain.Entities;

public class Vehicle : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string? VIN { get; set; }

    public User Client { get; set; } = null!;
    public ICollection<InterventionRequest> Interventions { get; set; } = new List<InterventionRequest>();
    public ICollection<SymptomReport> SymptomReports { get; set; } = new List<SymptomReport>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}