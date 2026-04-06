using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Partner;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý khách hàng
    /// </summary>
    public class CustomerController : Controller
    {
        private static readonly int PAGESIZE = 10;

        /// <summary>
        /// Hiển thị danh sách khách hàng (với tìm kiếm + phân trang)
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("CustomerSearch");
            if (input == null || Request.Query.ContainsKey("page") || Request.Query.ContainsKey("searchValue"))
            {
                input = new PaginationSearchInput()
                {
                    Page = page,
                    PageSize = PAGESIZE,
                    SearchValue = searchValue
                };
                ApplicationContext.SetSessionData("CustomerSearch", input);
            }

            var result = await PartnerDataService.ListCustomersAsync(input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form tạo khách hàng mới (GET)
        /// </summary>
        public IActionResult Create()
        {
            var model = new Customer()
            {
                CustomerID = 0,
                IsLocked = false
            };
            return View(model);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa khách hàng (GET)
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
            {
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        /// <summary>
        /// Lưu dữ liệu khách hàng (thêm mới hoặc cập nhật) (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Customer data)
        {
            try
            {
                // Validate dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");
                if (string.IsNullOrWhiteSpace(data.ContactName))
                    ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");
                if (string.IsNullOrWhiteSpace(data.Address))
                    ModelState.AddModelError(nameof(data.Address), "Địa chỉ không được để trống");

                // Kiểm tra email trùng
                if (!string.IsNullOrWhiteSpace(data.Email))
                {
                    bool isValidEmail = await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID);
                    if (!isValidEmail)
                        ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi khách hàng khác");
                }

                if (!ModelState.IsValid)
                {
                    // Nếu dữ liệu không hợp lệ, quay lại form
                    if (data.CustomerID == 0)
                        return View("Create", data);
                    else
                        return View("Edit", data);
                }

                if (data.CustomerID == 0)
                {
                    // Thêm mới
                    int? id = await PartnerDataService.AddCustomerAsync(data);
                    if (id == null || id <= 0)
                    {
                        TempData["ErrorMessage"] = "Không thể thêm khách hàng. Vui lòng thử lại.";
                        return View("Create", data);
                    }
                    TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                }
                else
                {
                    // Cập nhật
                    bool result = await PartnerDataService.UpdateCustomerAsync(data);
                    if (!result)
                    {
                        TempData["ErrorMessage"] = "Không thể cập nhật khách hàng. Vui lòng thử lại.";
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                if (data.CustomerID == 0)
                    return View("Create", data);
                else
                    return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa khách hàng (GET)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            // Kiểm tra xem khách hàng có dữ liệu liên quan không
            if (await PartnerDataService.IsUsedCustomerAsync(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa khách hàng này vì có dữ liệu liên quan (đơn hàng, ...)";
                return RedirectToAction("Index");
            }

            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
            {
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        /// <summary>
        /// Xử lý xóa khách hàng (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, Customer data)
        {
            try
            {
                bool result = await PartnerDataService.DeleteCustomerAsync(id);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Không thể xóa khách hàng. Khách hàng có thể đang có dữ liệu liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Hiển thị trang đổi mật khẩu cho khách hàng (GET)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
            {
                return RedirectToAction("Index");
            }
            return View(customer);
        }
    }
}
