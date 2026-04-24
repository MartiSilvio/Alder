using Alder.Test.Integration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alder.Test.Compilation.DynamicLinqEfCore;

[TestFixture]
[NonParallelizable]
public sealed partial class DynamicLinqEfCoreTests
{
    internal abstract class FixtureBase
    {
        private SqliteConnection? _connection;

        protected DbContextOptions<EfOrdersDbContext> DbOptions { get; private set; } = null!;

        [OneTimeSetUp]
        public void BaseSetUp()
        {
            AlderEval.Reset();
            AlderEval.Configure(o => o.UseCompiler());

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            DbOptions = new DbContextOptionsBuilder<EfOrdersDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var db = new EfOrdersDbContext(DbOptions);
            db.Database.EnsureCreated();
            db.Orders.AddRange(
                new EfOrder { Id = 1, Customer = "Alice", Total = 20m, IsActive = true, Discount = null, Notes = "vip" },
                new EfOrder { Id = 2, Customer = "Bob", Total = 80m, IsActive = true, Discount = 10m, Notes = null },
                new EfOrder { Id = 3, Customer = "Cara", Total = 120m, IsActive = false, Discount = null, Notes = "vip" },
                new EfOrder { Id = 4, Customer = "Ari", Total = 55m, IsActive = true, Discount = 5m, Notes = null });
            db.SaveChanges();
        }

        [OneTimeTearDown]
        public void BaseTearDown()
        {
            _connection?.Dispose();
            AlderEval.Reset();
        }
    }

    internal abstract class RelationshipFixtureBase
    {
        private SqliteConnection? _connection;

        protected DbContextOptions<EfCustomerOrdersDbContext> DbOptions { get; private set; } = null!;

        [OneTimeSetUp]
        public void BaseSetUp()
        {
            AlderEval.Reset();
            AlderEval.Configure(o => o.UseCompiler());

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            DbOptions = new DbContextOptionsBuilder<EfCustomerOrdersDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var db = new EfCustomerOrdersDbContext(DbOptions);
            db.Database.EnsureCreated();

            var alice = new EfCustomer { Id = 1, Name = "Alice" };
            var bob = new EfCustomer { Id = 2, Name = "Bob" };
            var cara = new EfCustomer { Id = 3, Name = "Cara" };

            db.Customers.AddRange(alice, bob, cara);
            db.CustomerOrders.AddRange(
                new EfCustomerOrder { Id = 1, CustomerId = 1, Product = "Laptop", Quantity = 1 },
                new EfCustomerOrder { Id = 2, CustomerId = 1, Product = "Mouse", Quantity = 2 },
                new EfCustomerOrder { Id = 3, CustomerId = 2, Product = "Keyboard", Quantity = 1 },
                new EfCustomerOrder { Id = 4, CustomerId = 3, Product = "Tablet", Quantity = 1 });
            db.SaveChanges();
        }

        [OneTimeTearDown]
        public void BaseTearDown()
        {
            _connection?.Dispose();
            AlderEval.Reset();
        }
    }
}

internal sealed class EfCustomerOrdersDbContext(DbContextOptions<EfCustomerOrdersDbContext> options) : DbContext(options)
{
    public DbSet<EfCustomer> Customers => Set<EfCustomer>();
    public DbSet<EfCustomerOrder> CustomerOrders => Set<EfCustomerOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EfCustomer>()
            .HasMany(customer => customer.Orders)
            .WithOne(order => order.Customer)
            .HasForeignKey(order => order.CustomerId);
    }
}

internal sealed class EfCustomer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<EfCustomerOrder> Orders { get; init; } = [];
}

internal sealed class EfCustomerOrder
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string Product { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public EfCustomer Customer { get; init; } = null!;
}
