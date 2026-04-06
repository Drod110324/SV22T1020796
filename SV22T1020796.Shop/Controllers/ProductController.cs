using Microsoft.AspNetCore.Mvc;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Shop.Controllers
{
    /// <summary>
    /// Controller hiển thị mặt hàng cho khách hàng
    /// </summary>
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        /// <summary>
        /// Danh sách sản phẩm (có lọc, tìm kiếm)
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchValue"></param>
        /// <param name="categoryID"></param>
        /// <param name="minPrice"></param>
        /// <param name="maxPrice"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "", int categoryID = 0, decimal minPrice = 0, decimal maxPrice = 0)
        {
            var input = new SV22T1020796.Models.Catalog.ProductSearchInput
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var data = await CatalogDataService.ListProductsAsync(input);

            ViewBag.SearchValue = input.SearchValue;
            ViewBag.CategoryID = input.CategoryID;
            ViewBag.MinPrice = input.MinPrice;
            ViewBag.MaxPrice = input.MaxPrice;

            return View(data);
        }

        /// <summary>
        /// Xem chi tiết sản phẩm
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Home");
            }
            
            // Lấy danh sách ảnh phụ
            var photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Photos = photos;
            
            // Lấy danh sách thuộc tính
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Attributes = attributes;
            
            // Lấy sản phẩm cùng loại
            var input = new SV22T1020796.Models.Catalog.ProductSearchInput
            {
                Page = 1,
                PageSize = 4,
                CategoryID = product.CategoryID ?? 0,
                SearchValue = ""
            };
            var relatedProducts = await CatalogDataService.ListProductsAsync(input);
            ViewBag.RelatedProducts = relatedProducts.DataItems.Where(p => p.ProductID != id).ToList();

            return View(product);
        }
    }
}
