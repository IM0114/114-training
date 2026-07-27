using OrderHub.Web.ViewModels;

namespace OrderHub.Tests;

public class LowStockProductRowViewModelTests
{
    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void IsCriticalStock_ReturnsTrueOnlyWhenStockIsBelowFive(int stockQuantity, bool expected)
    {
        var vm = new LowStockProductRowViewModel { StockQuantity = stockQuantity };

        Assert.Equal(expected, vm.IsCriticalStock);
    }
}
