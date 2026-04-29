using FluentAssertions;
using MecaManage.Application.Features.Auth.Commands;
using MecaManage.Tests.Helpers;
using Moq;
using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using Xunit;

namespace MecaManage.Tests.Features.Auth;

public class AuthTests
{
    [Fact]
    public async Task Register_Success()
    {
        var context = DbContextFactory.Create("Register_Success");
        SeedHelper.Seed(context);
        var handler = new RegisterCommandHandler(context);
        var command = new RegisterCommand("Test", "User", "test@test.tn", "Pass123!", "55000001");
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Fails()
    {
        var context = DbContextFactory.Create("Register_Duplicate");
        SeedHelper.Seed(context);
        var handler = new RegisterCommandHandler(context);
        var command = new RegisterCommand("Iheb", "Saafi", "iheb@mecamanage.tn", "Pass123!", "55000000");
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email");
    }

    [Fact]
    public async Task Login_Success()
    {
        var context = DbContextFactory.Create("Login_Success");
        SeedHelper.Seed(context);
        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<MecaManage.Domain.Entities.User>())).Returns("fake-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("fake-refresh");
        var handler = new LoginCommandHandler(context, jwtService.Object);
        var command = new LoginCommand("iheb@mecamanage.tn", "Admin123!");
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("fake-token");
    }

    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        var context = DbContextFactory.Create("Login_WrongPassword");
        SeedHelper.Seed(context);
        var jwtService = new Mock<IJwtService>();
        var handler = new LoginCommandHandler(context, jwtService.Object);
        var command = new LoginCommand("iheb@mecamanage.tn", "WrongPass!");
        var result = await handler.Handle(command, CancellationToken.None);
        result.Success.Should().BeFalse();
    }
}
