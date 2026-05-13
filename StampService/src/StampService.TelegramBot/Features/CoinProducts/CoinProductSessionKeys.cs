namespace StampService.TelegramBot.Features.CoinProducts;

public static class CoinProductSessionKeys
{
    public const string SelectedProductId = "coin_product.selected_product_id";
    public const string SelectedProductName = "coin_product.selected_product_name";
    public const string SelectedProductPrice = "coin_product.selected_product_price";

    public const string CreateName = "coin_product.create.name";
    public const string CreatePrice = "coin_product.create.price";

    public const string EditName = "coin_product.edit.name";
    public const string EditPrice = "coin_product.edit.price";

    public const string PurchaseRedemptionCode = "coin_product.purchase.redemption_code";
    public const string PurchaseProductId = "coin_product.purchase.product_id";
    public const string PurchaseProductName = "coin_product.purchase.product_name";
    public const string PurchaseProductPrice = "coin_product.purchase.product_price";
    public const string PurchaseCurrentBalance = "coin_product.purchase.current_balance";
    public const string PurchaseCustomerName = "coin_product.purchase.customer_name";
}
