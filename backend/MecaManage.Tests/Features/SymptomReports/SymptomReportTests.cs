using FluentAssertions;
using MecaManage.Application.Common.Interfaces;
using MecaManage.Application.Common.Models;
using MecaManage.Application.Features.SymptomReports.Commands;
using MecaManage.Domain.Enums;
using MecaManage.Tests.Helpers;
using Moq;
using Xunit;

namespace MecaManage.Tests.Features.SymptomReports;

public class SymptomReportTests
{
    [Fact]
    public async Task CreateSymptomReport_WithIADiagnosis_SavesDiagnosisData()
    {
        var context = DbContextFactory.Create("CreateSymptomReport_WithIADiagnosis");
        SeedHelper.Seed(context);

        var iaService = new Mock<IIAService>();
        iaService.Setup(x => x.GetDiagnosisAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            null,
            null)).ReturnsAsync(new IADiagnosisResponse
            {
                Diagnosis = "Suspicion de problème de batterie",
                ConfidenceScore = 0.91f,
                RecommendedWorkshop = "electrique",
                UrgencyLevel = "modere",
                EstimatedCostRange = "80-200 TND",
                RecommendedActions = "Vérifier la batterie | Tester l'alternateur",
                RagSourcesUsed = 4
            });

        var handler = new CreateSymptomReportCommandHandler(context, iaService.Object);
        var command = new CreateSymptomReportCommand(
            SeedHelper.UserId,
            SeedHelper.VehicleId,
            "Le moteur démarre mal et les lumières faiblissent");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ReportId.Should().NotBeNull();

        var report = context.SymptomReports.Single();
        report.AIPredictedIssue.Should().Be("Suspicion de problème de batterie");
        report.AIConfidenceScore.Should().Be(0.91f);
        report.AIRecommendations.Should().Contain("Vérifier la batterie");
        report.Status.Should().Be(SymptomReportStatus.PendingReview);
    }

    [Fact]
    public async Task CreateSymptomReport_ForwardsChefAtelierRoutingMetadataToIAService()
    {
        var context = DbContextFactory.Create("CreateSymptomReport_ForwardsChefAtelierRoutingMetadataToIAService");
        SeedHelper.Seed(context);

        var chefAtelierId = Guid.Parse("c8b48d9e-8e8a-4f63-8d27-8ebd2e3c7a21");
        var iaService = new Mock<IIAService>();
        iaService.Setup(x => x.GetDiagnosisAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            chefAtelierId,
            SeedHelper.GarageId)).ReturnsAsync((IADiagnosisResponse?)null);

        var handler = new CreateSymptomReportCommandHandler(context, iaService.Object);
        var command = new CreateSymptomReportCommand(
            SeedHelper.UserId,
            SeedHelper.VehicleId,
            "Le moteur ne démarre plus et le diagnostic doit être routé",
            SeedHelper.GarageId,
            chefAtelierId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        iaService.Verify(x => x.GetDiagnosisAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            chefAtelierId,
            SeedHelper.GarageId), Times.Once);
    }
}

