using System;
using Microsoft.EntityFrameworkCore;

namespace SoftFluent.EntityFrameworkCore.DataEncryption.Test.Context;

public class DatabaseContextFunc : DbContext
{
    private readonly Func<IEncryptionProvider> _encryptionProvider;

    public DbSet<AuthorEntity> Authors { get; set; }

    public DbSet<BookEntity> Books { get; set; }

    public DatabaseContextFunc(DbContextOptions options)
        : base(options)
    { }

    public DatabaseContextFunc(DbContextOptions options, Func<IEncryptionProvider> encryptionProviderFunc = null)
        : base(options)
    {
        _encryptionProvider = encryptionProviderFunc;
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseEncryption(_encryptionProvider);
    }
}
