using Microsoft.AspNetCore.Mvc;
using SV22T1020796.BusinessLayers;
using SV22T1020796.Models.Sales;
using SV22T1020796.Shop.Models;

namespace SV22T1020796.Shop.Controllers
{
    /// <summary>
    /// Controller quản lý giỏ hàng của khách hàng
    /// </summary>
    public class CartController : Controller
    {
        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập mới xem được giỏ hàng của mình.";
                return RedirectToAction("Login", "Account");
            }
            var cart = ShoppingCartService.GetCartItems();
            return View(cart);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int quantity = 1)
        {
            var product = await CatalogDataService.GetProductAsync(productID);
            if (product != null)
            {
                var item = new OrderDetailViewInfo
                {
                    ProductID = productID,
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    Photo = product.Photo ?? "",
                    Quantity = quantity,
                    SalePrice = product.Price
                };
                ShoppingCartService.AddToCart(item);
                var cart = ShoppingCartService.GetCartItems();
                return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity) });
            }
            return Json(new { success = false, message = "Sản phẩm không tồn tại" });
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult RemoveFromCart(int productID)
        {
            ShoppingCartService.RemoveFromCart(productID);
            return Json(new { success = true });
        }

        /// <summary>
        /// Xóa sạch giỏ hàng
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ClearCart()
        {
            ShoppingCartService.ClearCart();
            return Json(new { success = true });
        }
    }
}
