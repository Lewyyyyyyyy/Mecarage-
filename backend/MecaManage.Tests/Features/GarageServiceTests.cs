using FluentAssertions;
using MecaManage.Domain.Entities;
using MecaManage.Infrastructure.Persistence;
using MecaManage.Tests.Helpers;

namespace MecaManage.Tests.Features;

public class GarageServiceTests
{
    private readonly ApplicationDbContext _context;

    public GarageServiceTests()
    {
        _context = DatabaseHelper.GetInMemoryDbContext();
    }

    [Fact]
    public void GetGarageById_WithValidId_ShouldReturnGarage()
    {
        // Arrange
        var garageId = SeedHelper.GarageId;
        var garage = _context.Garages.FirstOrDefault(g => g.Id == garageId);

        // Act & Assert
        garage.Should().NotBeNull();
        garage.Id.Should().Be(garageId);
        garage.Name.Should().Be("Atelier Central");
    }

    [Fact]
    public void CreateGarage_WithValidData_ShouldSucceed()
    {
        // Arrange
        var newGarage = new Garage
        {
            Id = Guid.NewGuid(),
            Name = "Garage Test",
            Address = "Test Address",
            City = "Test City",
            Phone = "71111111",
            IsActive = true
        };

        // Act
        _context.Garages.Add(newGarage);
        _context.SaveChanges();

        // Assert
        var savedGarage = _context.Garages.FirstOrDefault(g => g.Id == newGarage.Id);
        savedGarage.Should().NotBeNull();
        savedGarage.Name.Should().Be("Garage Test");
    }

    [Fact]
    public void GetAllGarages_ShouldReturnAllGarages()
    {
        // Act
        var garages = _context.Garages.ToList();

        // Assert
        garages.Should().NotBeEmpty();
        garages.Count().Should().BeGreaterThan(0);
    }
}

