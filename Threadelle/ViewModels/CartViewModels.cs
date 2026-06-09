using Threadelle.Helpers;
using Threadelle.Models;
using Threadelle.Services;

namespace Threadelle.ViewModels
{
    public class CartViewModel
    {
        public List<CartLineViewModel> Lines { get; set; } = new();

        public decimal SubTotal         => Lines.Sum(l => l.LineTotal);
        public decimal ShippingThreshold { get; } = 50m;
        public decimal Shipping          => Lines.Count == 0 || SubTotal >= ShippingThreshold ? 0m : 5m;
        public decimal Total             => SubTotal + Shipping;
        public int     ItemCount         => Lines.Sum(l => l.Quantity);
        public bool    IsEmpty           => Lines.Count == 0;

        /// <summary>
        /// Build a CartViewModel.
        /// Pass <paramref name="pricing"/> to apply product-coupon discounts to unit prices.
        /// </summary>
        public static CartViewModel FromCart(
            Cart? cart,
            Dictionary<int, ProductPriceInfo>? pricing = null)
        {
            var vm = new CartViewModel();
            if (cart == null) return vm;

            foreach (var item in cart.CartItems.OrderBy(i => i.Id))
            {
                if (item.Product == null) continue;

                var basePrice    = item.Product.CalculatePrice();
                var priceInfo    = pricing?.GetValueOrDefault(item.ProductId);
                var effectivePrice = priceInfo?.EffectivePrice ?? basePrice;

                vm.Lines.Add(new CartLineViewModel
                {
                    CartItemId        = item.Id,
                    ProductId         = item.ProductId,
                    Name              = item.Product.Name,
                    Slug              = item.Product.Slug,
                    ImageUrl          = item.Product.PrimaryImageUrl(),
                    ColorId           = item.ColorId,
                    ColorName         = item.Color?.Name,
                    ColorHex          = item.Color?.HexCode,
                    OriginalUnitPrice = basePrice,
                    UnitPrice         = effectivePrice,
                    DiscountBadge     = priceInfo?.DiscountBadge,
                    Quantity          = item.Quantity,
                    MaxQuantity       = item.Product.IsOnePiece ? 1 : Math.Max(1, item.Product.Quantity)
                });
            }
            return vm;
        }
    }

    public class CartLineViewModel
    {
        public int     CartItemId        { get; set; }
        public int     ProductId         { get; set; }
        public string  Name              { get; set; } = string.Empty;
        public string  Slug              { get; set; } = string.Empty;
        public string? ImageUrl          { get; set; }
        public int?    ColorId           { get; set; }
        public string? ColorName         { get; set; }
        public string? ColorHex          { get; set; }

        /// <summary>Price before any product coupon discount.</summary>
        public decimal OriginalUnitPrice { get; set; }

        /// <summary>Effective unit price (discounted when a product coupon applies).</summary>
        public decimal UnitPrice         { get; set; }

        public string? DiscountBadge     { get; set; }
        public bool    HasDiscount       => UnitPrice < OriginalUnitPrice;
        public decimal SavedPerUnit      => OriginalUnitPrice - UnitPrice;

        public int     Quantity    { get; set; }
        public int     MaxQuantity { get; set; }

        public decimal LineTotal         => UnitPrice * Quantity;
        public decimal OriginalLineTotal => OriginalUnitPrice * Quantity;
    }
}
