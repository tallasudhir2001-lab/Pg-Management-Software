using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Services;

public class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        // 2️⃣ Read connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        // 👇 Dummy current user for design-time
        ICurrentUserService currentUserService = new DesignTimeCurrentUserService();

        return new ApplicationDbContext(optionsBuilder.Options, currentUserService);
    }
}
