using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_UsesThreshold_ReturnsOnlyProductsBelowThreshold()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-LOW", stock: 5);
        TestSetup.AddProduct(db, sku: "SKU-EDGE", stock: 10);
        TestSetup.AddProduct(db, sku: "SKU-HIGH", stock: 11);

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { "SKU-LOW" }, products.Select(p => p.Sku));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-ACTIVE", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-INACTIVE", stock: 3, isActive: false);

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { "SKU-ACTIVE" }, products.Select(p => p.Sku));
        Assert.All(products, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetLowStock_ReturnsProductsOrderedByStockQuantity()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-SEVEN", stock: 7);
        TestSetup.AddProduct(db, sku: "SKU-TWO", stock: 2);
        TestSetup.AddProduct(db, sku: "SKU-FIVE", stock: 5);

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { "SKU-TWO", "SKU-FIVE", "SKU-SEVEN" }, products.Select(p => p.Sku));
    }

    [Fact]
    public async Task GetLowStock_SoldQuantityLast30Days_ExcludesCancelledOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 5);
        var createdAt = DateTime.UtcNow.AddDays(-1);

        db.Orders.AddRange(
            CreateOrder(customer.Id, product.Id, OrderStatus.Confirmed, createdAt, 3),
            CreateOrder(customer.Id, product.Id, OrderStatus.Cancelled, createdAt, 4));
        db.SaveChanges();

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(3, products.Single().SoldQuantityLast30Days);
    }

    [Fact]
    public async Task GetLowStock_SoldQuantityLast30Days_ExcludesOrdersOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 5);
        var since = DateTime.UtcNow.AddDays(-30);

        db.Orders.AddRange(
            CreateOrder(customer.Id, product.Id, OrderStatus.Confirmed, since.AddDays(1), 2),
            CreateOrder(customer.Id, product.Id, OrderStatus.Confirmed, since.AddDays(-1), 5));
        db.SaveChanges();

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(2, products.Single().SoldQuantityLast30Days);
    }

    private static Order CreateOrder(int customerId, int productId, OrderStatus status, DateTime createdAt, int quantity)
    {
        return new Order
        {
            CustomerId = customerId,
            Status = status,
            CreatedAt = createdAt,
            Items =
            {
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPriceSnapshot = 100m
                }
            }
        };
    }
}
