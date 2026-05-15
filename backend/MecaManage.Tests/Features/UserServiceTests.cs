using FluentAssertions;
using MecaManage.Domain.Entities;
using MecaManage.Domain.Enums;
using MecaManage.Infrastructure.Persistence;
using MecaManage.Tests.Helpers;

namespace MecaManage.Tests.Features;

public class UserServiceTests
{
    private readonly ApplicationDbContext _context;

    public UserServiceTests()
    {
        _context = DatabaseHelper.GetInMemoryDbContext();
    }

    [Fact]
    public void GetUserById_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = SeedHelper.UserId;

        // Act
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be("iheb@mecamanage.tn");
        user.Role.Should().Be(UserRole.SuperAdmin);
    }

    [Fact]
    public void CreateUser_WithValidData_ShouldSucceed()
    {
        // Arrange
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@mecamanage.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            Phone = "71234567",
            Role = UserRole.Client,
            IsActive = true
        };

        // Act
        _context.Users.Add(newUser);
        _context.SaveChanges();

        // Assert
        var savedUser = _context.Users.FirstOrDefault(u => u.Id == newUser.Id);
        savedUser.Should().NotBeNull();
        savedUser.FirstName.Should().Be("Test");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var user = _context.Users.FirstOrDefault(u => u.Id == SeedHelper.UserId);
        var password = "Admin123!";

        // Act & Assert
        user.Should().NotBeNull();
        if (user != null)
        {
            var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            isValid.Should().BeTrue();
        }
    }
}

