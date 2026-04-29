using FluentAssertions;
using MecaManage.Application.Common.Interfaces;
using MecaManage.Application.Common.Models;
using MecaManage.Application.Features.Interventions.Commands;
using MecaManage.Domain.Enums;
using MecaManage.Tests.Helpers;
using Moq;
using Xunit;

namespace MecaManage.Tests.Features.Interventions;

public class InterventionTests
{
    [Fact]
    public async Task CreateIntervention_Success()
    {
        var context = DbContextFactory.Create("CreateIntervention_Success");
        SeedHelper.Seed(context);
        var handler = new CreateInterventionCommandHandler(context);
        var command = new CreateInterventionCommand(
            SeedHelper.UserId,
            SeedHelper.VehicleId, SeedHelper.GarageId,
            "Bruit moteur", UrgencyLevel.Urgent, null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.InterventionId.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateIntervention_VehicleNotFound_Fails()
    {
        var context = DbContextFactory.Create("CreateIntervention_VehicleNotFound");
        SeedHelper.Seed(context);
        var handler = new CreateInterventionCommandHandler(context);
        var command = new CreateInterventionCommand(
            SeedHelper.UserId,
            Guid.NewGuid(),
            SeedHelper.GarageId,
            "Bruit moteur",
            UrgencyLevel.Urgent,
            null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DiagnoseIntervention_IAServiceFailure_Fails()
    {
        var context = DbContextFactory.Create("DiagnoseIntervention_IAFailure");
        SeedHelper.Seed(context);
        var iaService = new Mock<IIAService>();
        iaService.Setup(x => x.GetDiagnosisAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            null,
            null)).ReturnsAsync((IADiagnosisResponse?)null);
        var handler = new DiagnoseInterventionCommandHandler(context, iaService.Object);
        var command = new DiagnoseInterventionCommand(SeedHelper.InterventionId);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeFalse();
    }
}
