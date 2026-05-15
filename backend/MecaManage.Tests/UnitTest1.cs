using FluentAssertions;
using MecaManage.Tests.Helpers;

namespace MecaManage.Tests;

public class UnitTest1
{
    [Fact]
    public void SeedConstants_ShouldHaveStableIdentifiers()
    {
        SeedHelper.UserId.Should().NotBe(Guid.Empty);
        SeedHelper.GarageId.Should().NotBe(Guid.Empty);
        SeedHelper.VehicleId.Should().NotBe(Guid.Empty);
    }
}