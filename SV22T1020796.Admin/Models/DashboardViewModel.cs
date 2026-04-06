using SV22T1020796.Models.Sales;

namespace SV22T1020796.Admin.Models
{
    /// <summary>
    /// ViewModel for Dashboard
    /// </summary>
    public class DashboardViewModel
    {
        /// <summary>
        /// Doanh thu hôm nay
        /// </summary>
        public decimal TodayRevenue { get; set; }
        /// <summary>
        /// Số lượng đơn hàng
        /// </summary>
        public int OrderCount { get; set; }
        /// <summary>
        /// Số lượng khách hàng
        /// </summary>
        public int CustomerCount { get; set; }
        /// <summary>
        /// Số lượng mặt hàng
        /// </summary>
        public int ProductCount { get; set; }
        /// <summary>
        /// Các đơn hàng đang xử lý
        /// </summary>
        public List<OrderViewInfo> ProcessingOrders { get; set; } = new List<OrderViewInfo>();
        /// <summary>
        /// Các sản phẩm bán chạy nhất
        /// </summary>
        public List<TopProductInfo> TopSellingProducts { get; set; } = new List<TopProductInfo>();
    }

    /// <summary>
    /// Thông tin sản phẩm bán chạy
    /// </summary>
    public class TopProductInfo
    {
        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string ProductName { get; set; } = "";
        /// <summary>
        /// Số lượng đã bán
        /// </summary>
        public int TotalSold { get; set; }
    }
}
