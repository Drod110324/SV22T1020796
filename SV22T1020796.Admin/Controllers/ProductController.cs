using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Catalog;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý mặt hàng
    /// </summary>
    public class ProductController : Controller
    {
        private static readonly int PAGESIZE = 10;

        /// <summary>
        /// Hiển thị danh sách mặt hàng (với tìm kiếm + phân trang + lọc)
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "",
            int categoryID = 0, int supplierID = 0, decimal minPrice = 0, decimal maxPrice = 0)
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>("ProductSearch");
            if (input == null || Request.Query.ContainsKey("page") || Request.Query.ContainsKey("searchValue")
                || Request.Query.ContainsKey("categoryID") || Request.Query.ContainsKey("supplierID")
                || Request.Query.ContainsKey("minPrice") || Request.Query.ContainsKey("maxPrice"))
            {
                input = new ProductSearchInput()
                {
                    Page = page,
                    PageSize = PAGESIZE,
                    SearchValue = searchValue,
                    CategoryID = categoryID,
                    SupplierID = supplierID,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice
                };
                ApplicationContext.SetSessionData("ProductSearch", input);
            }

            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 9999, SearchValue = "" });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 9999, SearchValue = "" });

            var result = await CatalogDataService.ListProductsAsync(input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form bổ sung mặt hàng mới
        /// </summary>
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            await LoadDropdowns();
            var model = new Product()
            {
                ProductID = 0,
                IsSelling = true
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần chỉnh sửa</param>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật mặt hàng";
            await LoadDropdowns();
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

        /// <summary>
        /// Lưu dữ liệu mặt hàng (thêm mới hoặc cập nhật) (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");
                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Đơn vị tính không được để trống");
                if (data.CategoryID <= 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");
                if (data.SupplierID <= 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");
                if (data.Price < 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá không được âm");

                if (!ModelState.IsValid)
                {
                    await LoadDropdowns();
                    ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật mặt hàng";
                    if (data.ProductID > 0)
                    {
                        ViewBag.Photos = await CatalogDataService.ListPhotosAsync(data.ProductID);
                        ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(data.ProductID);
                    }
                    return View("Edit", data);
                }

                if (uploadPhoto != null && uploadPhoto.Length > 0)
                {
                    string fileName = $"prod_{DateTime.Now.Ticks}{Path.GetExtension(uploadPhoto.FileName)}";
                    string filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
                    string? directoryPath = Path.GetDirectoryName(filePath);
                    if (directoryPath != null)
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                else if (string.IsNullOrEmpty(data.Photo))
                {
                    data.Photo = "nophoto.png";
                }

                if (data.ProductID == 0)
                {
                    int id = await CatalogDataService.AddProductAsync(data);
                    if (id <= 0)
                    {
                        TempData["ErrorMessage"] = "Không thể thêm mặt hàng. Vui lòng thử lại.";
                        await LoadDropdowns();
                        ViewBag.Title = "Bổ sung mặt hàng";
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Thêm mặt hàng thành công!";
                }
                else
                {
                    bool result = await CatalogDataService.UpdateProductAsync(data);
                    if (!result)
                    {
                        TempData["ErrorMessage"] = "Không thể cập nhật mặt hàng. Vui lòng thử lại.";
                        await LoadDropdowns();
                        ViewBag.Title = "Cập nhật mặt hàng";
                        ViewBag.Photos = await CatalogDataService.ListPhotosAsync(data.ProductID);
                        ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(data.ProductID);
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật mặt hàng thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                await LoadDropdowns();
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật mặt hàng";
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa mặt hàng (GET)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (await CatalogDataService.IsUsedProductAsync(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa mặt hàng này vì có dữ liệu liên quan (đơn hàng, ...)";
                return RedirectToAction("Index");
            }

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index");
            }
            return View(product);
        }

        /// <summary>
        /// Xử lý xóa mặt hàng (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, Product data)
        {
            try
            {
                bool result = await CatalogDataService.DeleteProductAsync(id);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Không thể xóa mặt hàng. Có thể đang có dữ liệu liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa mặt hàng thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // --- QUẢN LÝ THUỘC TÍNH SẢN PHẨM ---

        /// <summary>
        /// Hiển thị form thêm thuộc tính cho mặt hàng
        /// </summary>
        public async Task<IActionResult> CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính cho mặt hàng";
            var model = new ProductAttribute()
            {
                AttributeID = 0,
                ProductID = id
            };
            var product = await CatalogDataService.GetProductAsync(id);
            ViewBag.ProductName = product?.ProductName;
            return View("EditAttribute", model);
        }

        /// <summary>
        /// Cập nhật thuộc tính của một mặt hàng
        /// </summary>
        public async Task<IActionResult> EditAttribute(int id, int attributeId)
        {
            ViewBag.Title = "Cập nhật thuộc tính của mặt hàng";
            var attribute = await CatalogDataService.GetAttributeAsync(attributeId);
            if (attribute == null)
                return RedirectToAction("Edit", new { id });
            var product = await CatalogDataService.GetProductAsync(id);
            ViewBag.ProductName = product?.ProductName;
            return View(attribute);
        }

        /// <summary>
        /// Lưu thuộc tính (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.AttributeName))
                    ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");
                if (string.IsNullOrWhiteSpace(data.AttributeValue))
                    ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
                    var product = await CatalogDataService.GetProductAsync(data.ProductID);
                    ViewBag.ProductName = product?.ProductName;
                    return View("EditAttribute", data);
                }

                if (data.AttributeID == 0)
                {
                    await CatalogDataService.AddAttributeAsync(data);
                    TempData["SuccessMessage"] = "Thêm thuộc tính thành công!";
                }
                else
                {
                    await CatalogDataService.UpdateAttributeAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật thuộc tính thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        /// <summary>
        /// Xóa thuộc tính khỏi mặt hàng
        /// </summary>
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            try
            {
                await CatalogDataService.DeleteAttributeAsync(attributeId);
                TempData["SuccessMessage"] = "Xóa thuộc tính thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Edit", new { id });
        }

        /// <summary>
        /// Hiển thị form thêm ảnh cho mặt hàng
        /// </summary>
        public async Task<IActionResult> CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung ảnh cho mặt hàng";
            var model = new ProductPhoto()
            {
                PhotoID = 0,
                ProductID = id
            };
            var product = await CatalogDataService.GetProductAsync(id);
            ViewBag.ProductName = product?.ProductName;
            return View("EditPhoto", model);
        }

        /// <summary>
        /// Cập nhật thông tin ảnh sản phẩm
        /// </summary>
        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            ViewBag.Title = "Cập nhật ảnh của mặt hàng";
            var photo = await CatalogDataService.GetPhotoAsync(photoId);
            if (photo == null)
                return RedirectToAction("Edit", new { id });
            var product = await CatalogDataService.GetProductAsync(id);
            ViewBag.ProductName = product?.ProductName;
            return View(photo);
        }

        /// <summary>
        /// Lưu ảnh (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.Description))
                    ModelState.AddModelError(nameof(data.Description), "Mô tả ảnh không được để trống");

                // Xử lý upload ảnh
                if (uploadPhoto != null && uploadPhoto.Length > 0)
                {
                    string fileName = $"prodphoto_{DateTime.Now.Ticks}{Path.GetExtension(uploadPhoto.FileName)}";
                    string filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);
                    string? directoryPath = Path.GetDirectoryName(filePath);
                    if (directoryPath != null)
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                else if (data.PhotoID == 0 && string.IsNullOrEmpty(data.Photo))
                {
                    ModelState.AddModelError("uploadPhoto", "Vui lòng chọn ảnh cho thư viện");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh" : "Cập nhật ảnh";
                    var product = await CatalogDataService.GetProductAsync(data.ProductID);
                    ViewBag.ProductName = product?.ProductName;
                    return View("EditPhoto", data);
                }

                if (data.PhotoID == 0)
                {
                    await CatalogDataService.AddPhotoAsync(data);
                    TempData["SuccessMessage"] = "Thêm ảnh thành công!";
                }
                else
                {
                    await CatalogDataService.UpdatePhotoAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật ảnh thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        /// <summary>
        /// Xóa ảnh khỏi thư viện của mặt hàng
        /// </summary>
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            try
            {
                await CatalogDataService.DeletePhotoAsync(photoId);
                TempData["SuccessMessage"] = "Xóa ảnh thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Edit", new { id });
        }

        /// <summary>
        /// Load danh sách loại hàng và nhà cung cấp cho dropdown
        /// </summary>
        private async Task LoadDropdowns()
        {
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 9999, SearchValue = "" });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput { Page = 1, PageSize = 9999, SearchValue = "" });
        }

        /// <summary>
        /// Danh sách thuộc tính của mặt hàng
        /// </summary>
        public IActionResult ListAttributes(int id)
        {
            return RedirectToAction("Edit", new { id = id, _anchor = "attributes" });
        }

        /// <summary>
        /// Danh sách ảnh của mặt hàng
        /// </summary>
        public IActionResult ListPhotos(int id)
        {
            return RedirectToAction("Edit", new { id = id, _anchor = "photos" });
        }
    }
}