using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SV22T1020796.BusinessLayers;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Sales;
using SV22T1020796.Shop.Models;

namespace SV22T1020796.Shop.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng của khách hàng
    /// </summary>
    [Authorize(Roles = "customer")]
    public class OrderController : Controller
    {
        /// <summary>
        /// Danh sách đơn hàng đã mua
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            string? customerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerID))
            {
                 return RedirectToAction("Login", "Account");
            }

            var input = new OrderSearchInput
            {
                Page = 1,
                PageSize = 100,
                Status = (OrderStatusEnum)0, // Tất cả trạng thái
                SearchValue = ""
            };
            
            // Lấy danh sách đơn hàng
            var result = await SalesDataService.ListOrdersAsync(input);
            // Lọc lại chỉ lấy của khách hàng này (vì ListOrders thường là cho Admin tìm kiếm chung)
            var myOrders = result.DataItems.Where(o => o.CustomerID == customerID).ToList();

            return View(myOrders);
        }

        /// <summary>
        /// Trang thanh toán
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Checkout()
        {
            var cart = ShoppingCartService.GetCartItems();
            if (cart.Count == 0)
                return RedirectToAction("Index", "Cart");
            
            // Lấy thông tin khách hàng hiện tại
            string? customerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(customerIdClaim) && int.TryParse(customerIdClaim, out int customerID))
            {
                 var customer = await PartnerDataService.GetCustomerAsync(customerID);
                 ViewBag.Customer = customer;
            }

            return View(cart);
        }

        /// <summary>
        /// Xử lý đặt hàng
        /// </summary>
        /// <param name="customerName"></param>
        /// <param name="phone"></param>
        /// <param name="email"></param>
        /// <param name="province"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string customerName, string phone, string email, string province, string address)
        {
            var cart = ShoppingCartService.GetCartItems();
            if (cart.Count == 0)
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống." });

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(address))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin giao hàng." });

            // 1. Lấy mã khách hàng từ thông tin đăng nhập
            string? customerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out int customerID))
            {
                 return Json(new { success = false, message = "Không xác định được danh tính khách hàng. Vui lòng đăng nhập lại." });
            }

            // 2. Tạo đơn hàng (Trạng thái mặc định là "Vừa tạo")
            var order = new Order
            {
                CustomerID = customerID,
                OrderTime = DateTime.Now,
                DeliveryProvince = province,
                DeliveryAddress = address,
                EmployeeID = 1, // Gán tạm cho nhân viên hệ thống
                Status = OrderStatusEnum.New
            };

            int orderID = await SalesDataService.AddOrderAsync(order);
            if (orderID > 0)
            {
                // 3. Thêm chi tiết đơn hàng
                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(new OrderDetail
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }

                // 4. Xóa giỏ hàng
                ShoppingCartService.ClearCart();
                return Json(new { success = true, orderID = orderID });
            }

            return Json(new { success = false, message = "Không thể đặt hàng vào lúc này. Vui lòng thử lại sau." });
        }

        /// <summary>
        /// Xem chi tiết đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isSuccess"></param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int id, bool isSuccess = false)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            ViewBag.IsNewOrder = isSuccess;
            return View(order);
        }
    }
}
