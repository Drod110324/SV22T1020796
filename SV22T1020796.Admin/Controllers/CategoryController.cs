using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Catalog;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý loại hàng
    /// </summary>
    public class CategoryController : Controller
    {
        private static readonly int PAGESIZE = 10;

        /// <summary>
        /// Hiển thị danh sách loại hàng (với tìm kiếm + phân trang)
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("CategorySearch");
            if (input == null || Request.Query.ContainsKey("page") || Request.Query.ContainsKey("searchValue"))
            {
                input = new PaginationSearchInput()
                {
                    Page = page,
                    PageSize = PAGESIZE,
                    SearchValue = searchValue
                };
                ApplicationContext.SetSessionData("CategorySearch", input);
            }

            var result = await CatalogDataService.ListCategoriesAsync(input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form tạo loại hàng mới (GET)
        /// </summary>
        public IActionResult Create()
        {
            var model = new Category()
            {
                CategoryID = 0
            };
            return View(model);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa loại hàng (GET)
        /// </summary>
        /// <param name="id">Mã loại hàng cần cập nhật</param>
        public async Task<IActionResult> Edit(int id)
        {
            var category = await CatalogDataService.GetCategoryAsync(id);
            if (category == null)
            {
                return RedirectToAction("Index");
            }
            return View(category);
        }

        /// <summary>
        /// Lưu dữ liệu loại hàng (thêm mới hoặc cập nhật) (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Category data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

                if (!ModelState.IsValid)
                {
                    if (data.CategoryID == 0)
                        return View("Create", data);
                    else
                        return View("Edit", data);
                }

                if (data.CategoryID == 0)
                {
                    int id = await CatalogDataService.AddCategoryAsync(data);
                    if (id <= 0)
                    {
                        TempData["ErrorMessage"] = "Không thể thêm loại hàng. Vui lòng thử lại.";
                        return View("Create", data);
                    }
                    TempData["SuccessMessage"] = "Thêm loại hàng thành công!";
                }
                else
                {
                    bool result = await CatalogDataService.UpdateCategoryAsync(data);
                    if (!result)
                    {
                        TempData["ErrorMessage"] = "Không thể cập nhật loại hàng. Vui lòng thử lại.";
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật loại hàng thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                if (data.CategoryID == 0)
                    return View("Create", data);
                else
                    return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa loại hàng (GET)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (await CatalogDataService.IsUsedCategoryAsync(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa loại hàng này vì có dữ liệu liên quan (mặt hàng, ...)";
                return RedirectToAction("Index");
            }

            var category = await CatalogDataService.GetCategoryAsync(id);
            if (category == null)
            {
                return RedirectToAction("Index");
            }
            return View(category);
        }

        /// <summary>
        /// Xử lý xóa loại hàng (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, Category data)
        {
            try
            {
                bool result = await CatalogDataService.DeleteCategoryAsync(id);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Không thể xóa loại hàng. Có thể đang có dữ liệu liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa loại hàng thành công!";
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
