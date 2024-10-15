using Microsoft.EntityFrameworkCore;

namespace SoftFluent.AesSample.Fluent;

public class EncryptedDatabaseContext : DatabaseContext
{
    public EncryptedDatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options, null)
    {
    }
}