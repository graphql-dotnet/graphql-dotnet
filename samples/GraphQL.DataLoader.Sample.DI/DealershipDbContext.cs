using GraphQL.DataLoader.Sample.DI.Types;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Sample.DI;

public class DealershipDbContext : DbContext
{
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Salesperson> Salespeople => Set<Salesperson>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite(
                new SqliteConnectionStringBuilder()
                {
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    BrowsableConnectionString = true,
                    DataSource = "my.db"
                }
                .ToString()
            )
            .UseSeeding((ctx, _) =>
            {
                if (ctx.Set<Salesperson>().Any())
                    return;
                ctx.Set<Salesperson>().AddRange(_seedSalespeople);
                ctx.SaveChanges();
            })
            .UseAsyncSeeding(async (ctx, _, cancellationToken) =>
            {
                if (await ctx
                        .Set<Salesperson>()
                        .AnyAsync(cancellationToken)
                        )
                    return;
                await ctx
                    .Set<Salesperson>()
                    .AddRangeAsync(_seedSalespeople, cancellationToken)
                    ;
                await ctx
                    .SaveChangesAsync(cancellationToken)
                    ;
            });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Salesperson>()
            .HasMany(sp => sp.AssignedCars)
            .WithOne()
            .HasForeignKey(car => car.SalesPersonId);
        modelBuilder.Entity<Salesperson>().HasIndex(sp => sp.Name).IsUnique();
    }

    private static readonly Salesperson[] _seedSalespeople =
    [
        new()
        {
            Name = "John",
            AssignedCars =
            [
                Car.Create("Ford Focus"),
                Car.Create("Ford Focus"),
                Car.Create("Ford F150", price: 50_000),
                Car.Create("Ford F150", price: 45_000),
                Car.Create("Ford F250", price: 75_000)
            ]
        },
        new()
        {
            Name = "Oh Deer",
            AssignedCars =
            [
                Car.Create("Ford Fiesta", price: 15_000),
                Car.Create("Ford Fiesta", price: 15_000),
                Car.Create("Ford Fiesta", price: 15_000),
                Car.Create("Ford Fiesta", price: 15_000),
                Car.Create("Ford Fiesta", price: 15_000),
                Car.Create("Ford Fiesta", price: 15_000),
                Car.Create("Hyundai i10", price: 17_000),
                Car.Create("Hyundai i10", price: 17_000),
                Car.Create("Hyundai i10", price: 17_000),
                Car.Create("Hyundai i10", price: 17_000),
                Car.Create("Hyundai i10", price: 17_000)
            ]
        }
    ];
}
