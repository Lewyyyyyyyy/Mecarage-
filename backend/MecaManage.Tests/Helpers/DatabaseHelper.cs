using MecaManage.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Tests.Helpers;

public static class DatabaseHelper
{
    public static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        SeedHelper.SeedDatabase(context);

        return context;
    }
}

