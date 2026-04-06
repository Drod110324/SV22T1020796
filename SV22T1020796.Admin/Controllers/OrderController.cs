using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.BusinessLayers;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Sales;
using SV22T1020796.Models.Catalog;
using System.Globalization;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng
    /// </summary>
    public class OrderController : Controller
    {
        private const int ORDER_PAGE_SIZE = 15;
        private const int PRODUCT_PAGE_SIZE = 20;
        private const string ORDER_SEARCH_SESSION = "OrderSearchInput";
        private const string PRODUCT_SEARCH_SESSION = "ProductSearchForOrder";

        // --- QUẢN LÝ DANH SÁCH & TRA CỨU ---

        /// <summary>
        /// Hiển thị danh sách đơn hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH_SESSION);
            if (input == null)
            {
                input = new OrderSearchInput
                {
                    Page = 1,
                    PageSize = ORDER_PAGE_SIZE,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
                };
            }
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và lọc đơn hàng
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH_SESSION, input);
            return PartialView(result);
        }

        // --- LẬP ĐƠN HÀNG (GIỎ HÀNG) ---

        /// <summary>
        /// Hiển thị giao diện tạo đơn hàng (giỏ hàng)
        /// </summary>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_SESSION);
            if (input == null)
            {
                input = new ProductSearchInput
                {
                    Page = 1,
                    PageSize = PRODUCT_PAGE_SIZE,
                    SearchValue = ""
                };
            }
            else
            {
                // Luôn cập nhật lại đúng số lượng 20 sản phẩm mỗi trang
                input.PageSize = PRODUCT_PAGE_SIZE;
            }
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm mặt hàng để thêm vào đơn hàng
        /// </summary>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_SESSION, input);
            return PartialView(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        public IActionResult ShowShoppingCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return PartialView("ShowShoppingCart", cart);
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart(OrderDetailViewInfo item)
        {
            if (item.SalePrice <= 0 || item.Quantity <= 0)
                return Json("Giá bán và số lượng không hợp lệ");

            var cart = ShoppingCartService.GetShoppingCart();
            var existsItem = cart.Find(m => m.ProductID == item.ProductID);
            if (existsItem == null)
            {
                var product = await CatalogDataService.GetProductAsync(item.ProductID);
                if (product != null)
                {
                    item.ProductName = product.ProductName;
                    item.Unit = product.Unit;
                    item.Photo = product.Photo;
                    ShoppingCartService.AddCartItem(item);
                }
            }
            else
            {
                ShoppingCartService.AddCartItem(item);
            }

            return Json("");
        }

        /// <summary>
        /// Xóa mặt hàng khỏi giỏ hàng
        /// </summary>
        public IActionResult RemoveFromCart(int id)
        {
            ShoppingCartService.RemoveCartItem(id);
            return Json("");
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public IActionResult ClearCart()
        {
            ShoppingCartService.ClearCart();
            return Json("");
        }

        /// <summary>
        /// Cập nhật số lượng và giá của mặt hàng trong giỏ hàng
        /// </summary>
        [HttpPost]
        public IActionResult UpdateCartItem(int id, int quantity, decimal salePrice)
        {
            if (quantity <= 0 || salePrice <= 0)
                return Json("Số lượng và giá bán không hợp lệ");

            ShoppingCartService.UpdateCartItem(id, quantity, salePrice);
            return Json("");
        }

        /// <summary>
        /// Khởi tạo đơn hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Init(string customerName, string deliveryProvince, string address)
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
                return Json("Giỏ hàng trống. Vui lòng chọn mặt hàng.");

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(address))
                return Json("Vui lòng nhập đầy đủ thông tin khách hàng và nơi giao hàng.");

            // Tìm khách hàng theo tên (chính xác tuyệt đối)
            var customer = (await PartnerDataService.ListCustomersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 1,
                SearchValue = customerName
            })).DataItems.FirstOrDefault(c => c.CustomerName.Trim().ToLower() == customerName.Trim().ToLower());

            int customerID = 0;
            if (customer != null)
            {
                customerID = customer.CustomerID;
            }
            else
            {
                // Nếu không tìm thấy, tạo khách hàng mới với thông tin cơ bản
                customerID = await PartnerDataService.AddCustomerAsync(new SV22T1020796.Models.Partner.Customer
                {
                    CustomerName = customerName,
                    ContactName = customerName, // Dùng tạm tên khách hàng cho tên giao dịch
                    Province = deliveryProvince,
                    Address = address,
                    Email = "" // Để trống email
                });
            }

            if (customerID <= 0)
                return Json("Không thể xác định hoặc tạo khách hàng mới.");

            // Giả sử nhân viên đang đăng nhập ID = 1
            int employeeID = 1;

            Order order = new Order
            {
                CustomerID = customerID,
                OrderTime = DateTime.Now,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = address,
                EmployeeID = employeeID,
                Status = OrderStatusEnum.New
            };

            int orderID = await SalesDataService.AddOrderAsync(order);
            if (orderID > 0)
            {
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
                ShoppingCartService.ClearCart();
                return Json(new { success = true, orderID = orderID });
            }

            return Json("Không thể lập đơn hàng.");
        }

        // --- CHI TIẾT & XỬ LÝ NGHIỆP VỤ ---

        /// <summary>
        /// Hiển thị chi tiết đơn hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Details = details;

            return View(order);
        }

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public async Task<IActionResult> Accept(int id)
        {
            int employeeID = 1; 
            bool result = await SalesDataService.AcceptOrderAsync(id, employeeID);
            if (!result) TempData["ErrorMessage"] = "Không thể duyệt đơn hàng này.";
            else TempData["SuccessMessage"] = "Duyệt đơn hàng thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Chuyển trạng thái giao hàng
        /// </summary>
        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            if (Request.Method == "GET")
            {
                ViewBag.OrderID = id;
                return View();
            }

            if (shipperID <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn người giao hàng.";
                return RedirectToAction("Detail", new { id });
            }
            bool result = await SalesDataService.ShipOrderAsync(id, shipperID);
            if (!result) TempData["ErrorMessage"] = "Không thể chuyển trạng thái sang đang giao hàng.";
            else TempData["SuccessMessage"] = "Đã chuyển đơn hàng sang trạng thái đang giao hàng.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public async Task<IActionResult> Finish(int id)
        {
            bool result = await SalesDataService.CompleteOrderAsync(id);
            if (!result) TempData["ErrorMessage"] = "Không thể hoàn tất đơn hàng này.";
            else TempData["SuccessMessage"] = "Đơn hàng đã hoàn tất thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public async Task<IActionResult> Reject(int id)
        {
            int employeeID = 1;
            bool result = await SalesDataService.RejectOrderAsync(id, employeeID);
            if (!result) TempData["ErrorMessage"] = "Không thể từ chối đơn hàng này.";
            else TempData["SuccessMessage"] = "Đã từ chối đơn hàng thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public async Task<IActionResult> Cancel(int id)
        {
            bool result = await SalesDataService.CancelOrderAsync(id);
            if (!result) TempData["ErrorMessage"] = "Không thể hủy đơn hàng này.";
            else TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Xóa vĩnh viễn đơn hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (!result) TempData["ErrorMessage"] = "Không thể xóa đơn hàng này.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Chỉnh sửa mặt hàng trong đơn hàng (chỉ khi đơn hàng chưa được duyệt)
        /// </summary>
        public async Task<IActionResult> EditCartItem(int id, int productId)
        {
            var detail = await SalesDataService.GetDetailAsync(id, productId);
            return View(detail);
        }

        /// <summary>
        /// Cập nhật chi tiết mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(OrderDetail data)
        {
            if (data.Quantity <= 0 || data.SalePrice <= 0)
                return Json("Số lượng và giá bán không hợp lệ");
            
            bool result = await SalesDataService.UpdateDetailAsync(data);
            if (!result) return Json("Không thể cập nhật chi tiết đơn hàng");

            return Json("");
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteCartItem(int id, int productId)
        {
            bool result = await SalesDataService.DeleteDetailAsync(id, productId);
            if (!result) TempData["ErrorMessage"] = "Không thể xóa mặt hàng khỏi đơn hàng";
            return RedirectToAction("Detail", new { id });
        }
    }
}