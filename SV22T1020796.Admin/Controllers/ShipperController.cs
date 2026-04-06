using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Partner;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý người giao hàng
    /// </summary>
    public class ShipperController : Controller
    {
        private static readonly int PAGESIZE = 10;

        /// <summary>
        /// Hiển thị danh sách đơn vị vận chuyển (với tìm kiếm + phân trang)
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("ShipperSearch");
            if (input == null || Request.Query.ContainsKey("page") || Request.Query.ContainsKey("searchValue"))
            {
                input = new PaginationSearchInput()
                {
                    Page = page,
                    PageSize = PAGESIZE,
                    SearchValue = searchValue
                };
                ApplicationContext.SetSessionData("ShipperSearch", input);
            }

            var result = await PartnerDataService.ListShippersAsync(input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form thêm đơn vị vận chuyển mới (GET)
        /// </summary>
        public IActionResult Create()
        {
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View(model);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa đơn vị vận chuyển (GET)
        /// </summary>
        /// <param name="id">Mã đơn vị cần cập nhật</param>
        public async Task<IActionResult> Edit(int id)
        {
            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null)
            {
                return RedirectToAction("Index");
            }
            return View(shipper);
        }

        /// <summary>
        /// Lưu dữ liệu người giao hàng (thêm mới hoặc cập nhật) (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Shipper data)
        {
            try
            {
                // Validate dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");
                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");

                if (!ModelState.IsValid)
                {
                    if (data.ShipperID == 0)
                        return View("Create", data);
                    else
                        return View("Edit", data);
                }

                if (data.ShipperID == 0)
                {
                    // Thêm mới
                    int id = await PartnerDataService.AddShipperAsync(data);
                    if (id <= 0)
                    {
                        TempData["ErrorMessage"] = "Không thể thêm người giao hàng. Vui lòng thử lại.";
                        return View("Create", data);
                    }
                    TempData["SuccessMessage"] = "Thêm người giao hàng thành công!";
                }
                else
                {
                    // Cập nhật
                    bool result = await PartnerDataService.UpdateShipperAsync(data);
                    if (!result)
                    {
                        TempData["ErrorMessage"] = "Không thể cập nhật người giao hàng. Vui lòng thử lại.";
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật người giao hàng thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                if (data.ShipperID == 0)
                    return View("Create", data);
                else
                    return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa đơn vị vận chuyển (GET)
        /// </summary>
        /// <param name="id">Mã đơn vị cần xóa</param>
        public async Task<IActionResult> Delete(int id)
        {
            if (await PartnerDataService.IsUsedShipperAsync(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa người giao hàng này vì có dữ liệu liên quan (đơn hàng, ...)";
                return RedirectToAction("Index");
            }

            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null)
            {
                return RedirectToAction("Index");
            }
            return View(shipper);
        }

        /// <summary>
        /// Xử lý xóa đơn vị vận chuyển (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, Shipper data)
        {
            try
            {
                bool result = await PartnerDataService.DeleteShipperAsync(id);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Không thể xóa người giao hàng. Có thể đang có dữ liệu liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa người giao hàng thành công!";
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
