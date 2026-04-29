using MecaManage.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Tests.Helpers;

public static class DbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
