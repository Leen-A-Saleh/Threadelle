using Threadelle.Models;

namespace Threadelle.Helpers
{
    /// <summary>
    /// ThreadElle products are priced from the craft work behind them:
    /// labour (work hours * hourly rate) plus the cost of the materials used.
    /// These helpers compute the live price wherever a Product is shown.
    /// </summary>
    public static class PricingExtensions
    {
        public static decimal CalculatePrice(this Product product)
        {
            var labour = product.WorkHours * product.HourRate;

            decimal materials = 0m;
            if (product.ProductMaterials != null)
            {
                foreach (var pm in product.ProductMaterials)
                {
                    if (pm.Material != null)
                        materials += pm.QuantityUsed * pm.Material.PricePerUnit;
                }
            }

            return Math.Round(labour + materials, 2);
        }

        public static string? PrimaryImageUrl(this Product product)
        {
            if (product.Images == null || product.Images.Count == 0) return null;
            var primary = product.Images.FirstOrDefault(i => i.IsPrimary) ?? product.Images.OrderBy(i => i.DisplayOrder).First();
            return primary.ImageUrl;
        }
    }
}
