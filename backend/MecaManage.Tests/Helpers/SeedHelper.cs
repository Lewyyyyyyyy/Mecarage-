using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MecaManage.Infrastructure.Persistence;

namespace MecaManage.Tests.Helpers;

public static class SeedHelper
{
    public static readonly Guid UserId = Guid.Parse("261f01ec-5415-4345-b5cf-7613b84e6fdb");
    public static readonly Guid GarageId = Guid.Parse("a42e1fc7-82fd-4831-8c5e-5be77ba32054");
    public static readonly Guid VehicleId = Guid.Parse("dbf18dd0-f105-4317-b977-94e9c1f9e7ee");
    public static readonly Guid InterventionId = Guid.Parse("44954104-31dc-477f-b897-75a9a857b81c");

    public static void Seed(ApplicationDbContext context)
    {
        SeedDatabase(context);
    }

    public static void SeedDatabase(ApplicationDbContext context)
    {
        if (context.Users.Any())
            return;

        var user = new User
        {
            Id = UserId,
            FirstName = "Ihebeddine",
            LastName = "Saafi",
            Email = "iheb@mecamanage.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Phone = "55000000",
            Role = UserRole.SuperAdmin,
            IsActive = true
        };

        var garage = new Garage
        {
            Id = GarageId,
            Name = "Atelier Central",
            Address = "Rue de la République",
            City = "Tunis",
            Phone = "71000001",
            IsActive = true
        };

        var vehicle = new Vehicle
        {
            Id = VehicleId,
            ClientId = UserId,
            Brand = "Peugeot",
            Model = "208",
            Year = 2020,
            LicensePlate = "TU123456",
            FuelType = "Essence",
            Mileage = 45000
        };

        var intervention = new InterventionRequest
        {
            Id = InterventionId,
            ClientId = UserId,
            VehicleId = VehicleId,
            GarageId = GarageId,
            Description = "Bruit suspect au moteur",
            Status = InterventionStatus.EnAttente,
            UrgencyLevel = UrgencyLevel.Urgent
        };

        context.Users.Add(user);
        context.Garages.Add(garage);
        context.Vehicles.Add(vehicle);
        context.InterventionRequests.Add(intervention);
        context.SaveChanges();
    }
}
