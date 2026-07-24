namespace OrderHub.Web.ViewModels;

public class LowStockProductRowViewModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int SoldQuantityLast30Days { get; set; }
    public bool IsActive { get; set; }
}
