using OrderHub.Core.Domain;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<IReadOnlyList<LowStockProductReport>> GetLowStockAsync(int threshold, DateTime since);
    Task<Product?> GetByIdAsync(int id);
    Task SaveChangesAsync();
}
