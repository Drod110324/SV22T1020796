using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Partner;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý nhà cung cấp
    /// </summary>
    public class SupplierController : Controller
    {
        private static readonly int PAGESIZE = 10;

        /// <summary>
        /// Hiển thị danh sách nhà cung cấp (với tìm kiếm + phân trang)
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("SupplierSearch");
            if (input == null || Request.Query.ContainsKey("page") || Request.Query.ContainsKey("searchValue"))
            {
                input = new PaginationSearchInput()
                {
                    Page = page,
                    PageSize = PAGESIZE,
                    SearchValue = searchValue
                };
                ApplicationContext.SetSessionData("SupplierSearch", input);
            }

            var result = await PartnerDataService.ListSuppliersAsync(input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form thêm nhà cung cấp mới (GET)
        /// </summary>
        public async Task<IActionResult> Create()
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View(model);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa nhà cung cấp (GET)
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần cập nhật</param>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null)
            {
                return RedirectToAction("Index");
            }
            return View(supplier);
        }

        /// <summary>
        /// Lưu dữ liệu nhà cung cấp (thêm mới hoặc cập nhật) (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Supplier data)
        {
            try
            {
                // Validate dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");
                if (string.IsNullOrWhiteSpace(data.ContactName))
                    ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");
                if (string.IsNullOrWhiteSpace(data.Address))
                    ModelState.AddModelError(nameof(data.Address), "Địa chỉ không được để trống");
                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");

                // Kiểm tra email trùng
                if (!string.IsNullOrWhiteSpace(data.Email))
                {
                    bool isValidEmail = await PartnerDataService.ValidateSupplierEmailAsync(data.Email, data.SupplierID);
                    if (!isValidEmail)
                        ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhà cung cấp khác");
                }

                if (!ModelState.IsValid)
                {
                    // Load lại danh sách tỉnh thành khi quay lại form
                    ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                    if (data.SupplierID == 0)
                        return View("Create", data);
                    else
                        return View("Edit", data);
                }

                if (data.SupplierID == 0)
                {
                    // Thêm mới
                    int id = await PartnerDataService.AddSupplierAsync(data);
                    if (id <= 0)
                    {
                        TempData["ErrorMessage"] = "Không thể thêm nhà cung cấp. Vui lòng thử lại.";
                        ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                        return View("Create", data);
                    }
                    TempData["SuccessMessage"] = "Thêm nhà cung cấp thành công!";
                }
                else
                {
                    // Cập nhật
                    bool result = await PartnerDataService.UpdateSupplierAsync(data);
                    if (!result)
                    {
                        TempData["ErrorMessage"] = "Không thể cập nhật nhà cung cấp. Vui lòng thử lại.";
                        ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                if (data.SupplierID == 0)
                    return View("Create", data);
                else
                    return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa nhà cung cấp (GET)
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        public async Task<IActionResult> Delete(int id)
        {
            // Kiểm tra xem nhà cung cấp có dữ liệu liên quan không
            if (await PartnerDataService.IsUsedSupplierAsync(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này vì có dữ liệu liên quan (mặt hàng, ...)";
                return RedirectToAction("Index");
            }

            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null)
            {
                return RedirectToAction("Index");
            }
            return View(supplier);
        }

        /// <summary>
        /// Xử lý xóa nhà cung cấp (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, Supplier data)
        {
            try
            {
                bool result = await PartnerDataService.DeleteSupplierAsync(id);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp. Nhà cung cấp có thể đang có dữ liệu liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}
