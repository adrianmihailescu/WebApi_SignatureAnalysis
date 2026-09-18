using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = "Admin" },
                new User { Username = "support", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Support123!"), Role = "ReadOnly" },
                new User { Username = "analyst", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Analyst123!"), Role = "SecurityAnalyst" });
            await db.SaveChangesAsync();
        }

        if (!await db.Customers.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Customers.AddRange(
                new Customer { Name = "Acme Corporation", Importance = 10, CreatedAtUtc = now },
                new Customer { Name = "Contoso", Importance = 7, CreatedAtUtc = now },
                new Customer { Name = "Small Customer", Importance = 3, CreatedAtUtc = now });
            await db.SaveChangesAsync();
        }

        if (!await db.Signatures.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Signatures.AddRange(
                new Signature { Name = "Critical Malware", Priority = 10, ConditionsJson = """{"eventType":"malware","severity":"critical"}""", IsEnabled = true, CreatedAtUtc = now },
                new Signature { Name = "Suspicious Process", Priority = 7, ConditionsJson = """{"eventType":"suspicious_process"}""", IsEnabled = true, CreatedAtUtc = now },
                new Signature { Name = "Network Attack", Priority = 8, ConditionsJson = """{"eventType":"network_attack","severity":"high"}""", IsEnabled = true, CreatedAtUtc = now });
            await db.SaveChangesAsync();
        }
    }
}
