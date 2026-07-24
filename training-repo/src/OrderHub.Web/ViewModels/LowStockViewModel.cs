using System.ComponentModel.DataAnnotations;

namespace OrderHub.Web.ViewModels;

public class LowStockViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "庫存門檻必須大於 0")]
    public int Threshold { get; set; } = 10;

    public IReadOnlyList<LowStockProductRowViewModel> Products { get; set; } = Array.Empty<LowStockProductRowViewModel>();
}