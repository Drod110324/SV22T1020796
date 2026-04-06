using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.SQLServer;
using SV22T1020796.Models.Sales;

namespace SV22T1020796.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng thống kê dữ liệu cho Dashboard
    /// </summary>
    public static class DashboardDataService
    {
        /// <summary>
        /// Lấy tổng số lượng khách hàng
        /// </summary>
        public static async Task<int> GetCustomerCountAsync()
        {
            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers");
            }
        }

        /// <summary>
        /// Lấy tổng số lượng mặt hàng
        /// </summary>
        public static async Task<int> GetProductCountAsync()
        {
            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");
            }
        }

        /// <summary>
        /// Lấy tổng số lượng đơn hàng
        /// </summary>
        public static async Task<int> GetOrderCountAsync()
        {
            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
            }
        }

        /// <summary>
        /// Lấy doanh thu trong ngày hôm nay
        /// </summary>
        public static async Task<decimal> GetTodayRevenueAsync()
        {
            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT ISNULL(SUM(od.Quantity * od.SalePrice), 0)
                            FROM Orders o
                            JOIN OrderDetails od ON o.OrderID = od.OrderID
                            WHERE CAST(o.OrderTime AS DATE) = CAST(GETDATE() AS DATE)
                                  AND o.Status IN (2, 3, 4)"; // Accepted, Shipping, Completed
                return await connection.ExecuteScalarAsync<decimal>(sql);
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng cần xử lý (Mới hoặc Đã duyệt)
        /// </summary>
        public static async Task<List<OrderViewInfo>> ListProcessingOrdersAsync(int count = 10)
        {
            using (var connection = new SqlConnection(Configuration.ConnectionString))
            {
                await connection.OpenAsync();
                var sql = $@"SELECT TOP {count} o.*, e.FullName as EmployeeName, 
                                   c.CustomerName, c.ContactName as CustomerContactName, c.Email as CustomerEmail, c.Phone as CustomerPhone, c.Address as CustomerAddress,
                                   s.ShipperName, s.Phone as ShipperPhone,
                                   ISNULL((SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID), 0) as DetailsTotalValue
                            FROM Orders o
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE o.Status IN (1, 2) -- New, Accepted
                            ORDER BY o.OrderTime DESC";
                return (await connection.QueryAsync<OrderViewInfo>(sql)).ToList();
            }
        }
    }
}
