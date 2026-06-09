using Microsoft.AspNetCore.Mvc;
using Threadelle.Services;
using Threadelle.ViewModels;

namespace Threadelle.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService            _cart;
        private readonly IProductPricingService  _pricing;

        public CartController(ICartService cart, IProductPricingService pricing)
        {
            _cart    = cart;
            _pricing = pricing;
        }

        public async Task<IActionResult> Index()
        {
            var cart    = await _cart.GetCartAsync(includeItems: true);
            var pricing = await BuildPricingAsync(cart);
            return View(CartViewModel.FromCart(cart, pricing));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int? colorId, int quantity = 1)
        {
            int count;
            try { count = await _cart.AddAsync(productId, colorId, quantity); }
            catch (InvalidOperationException ex)
            {
                if (IsAjax) return Json(new { ok = false, message = ex.Message });
                TempData["FlashMessage"] = ex.Message;
                TempData["FlashType"]    = "danger";
                return RedirectToAction("Index");
            }

            if (IsAjax)
                return Json(new { ok = true, count, message = "Added to your bag" });

            TempData["FlashMessage"] = "Added to your bag";
            TempData["FlashType"]    = "success";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            await _cart.UpdateQuantityAsync(cartItemId, quantity);
            return await CartStateResult();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            await _cart.RemoveAsync(cartItemId);
            if (IsAjax) return await CartStateResult();
            TempData["FlashMessage"] = "Removed from your bag";
            TempData["FlashType"]    = "success";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Sidebar()
        {
            if (Request.Headers["X-Requested-With"] != "XMLHttpRequest")
                return RedirectToAction("Index");
            var cart    = await _cart.GetCartAsync(includeItems: true);
            var pricing = await BuildPricingAsync(cart);
            return PartialView("_CartSidebar", CartViewModel.FromCart(cart, pricing));
        }

        [HttpGet]
        public async Task<IActionResult> Count() =>
            Json(new { count = await _cart.GetItemCountAsync() });

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<IActionResult> CartStateResult()
        {
            var cart    = await _cart.GetCartAsync(includeItems: true);
            var pricing = await BuildPricingAsync(cart);
            var vm      = CartViewModel.FromCart(cart, pricing);
            var lines   = vm.Lines.ToDictionary(
                l => l.CartItemId,
                l => new { lineTotal = l.LineTotal, quantity = l.Quantity });
            return Json(new
            {
                ok       = true,
                count    = vm.ItemCount,
                subTotal = vm.SubTotal,
                shipping = vm.Shipping,
                total    = vm.Total,
                isEmpty  = vm.IsEmpty,
                lines
            });
        }

        private async Task<Dictionary<int, Threadelle.Services.ProductPriceInfo>?> BuildPricingAsync(
            Threadelle.Models.Cart? cart)
        {
            if (cart == null || !cart.CartItems.Any()) return null;
            var ids = cart.CartItems.Select(ci => ci.ProductId).Distinct();
            return await _pricing.GetEffectivePricesAsync(ids);
        }

        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
