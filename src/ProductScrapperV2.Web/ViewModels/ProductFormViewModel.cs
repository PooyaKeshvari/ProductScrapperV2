namespace ProductScrapperV2.Web.ViewModels;

public class ProductFormViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal OwnPrice { get; set; }
}
