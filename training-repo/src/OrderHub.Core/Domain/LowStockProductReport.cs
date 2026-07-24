namespace OrderHub.Core.Domain;

public class LowStockProductReport
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int SoldQuantityLast30Days { get; set; }
    public bool IsActive { get; set; }
}
