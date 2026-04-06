using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Admin.Models;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller cho trang chủ
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Trang chính của ứng dụng
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TodayRevenue = await DashboardDataService.GetTodayRevenueAsync(),
                OrderCount = await DashboardDataService.GetOrderCountAsync(),
                CustomerCount = await DashboardDataService.GetCustomerCountAsync(),
                ProductCount = await DashboardDataService.GetProductCountAsync(),
                ProcessingOrders = await DashboardDataService.ListProcessingOrdersAsync()
            };
            return View(model);
        }

        /// <summary>
        /// Hiển thị thông tin chính sách bảo mật
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]


        /// <summary>
        /// Hiển thị trang lỗi (Error)
        /// </summary>
        /// <return></return>
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
