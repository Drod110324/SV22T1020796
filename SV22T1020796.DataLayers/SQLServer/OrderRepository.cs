using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Sales;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho đơn hàng (Order) trên SQL Server
    /// </summary>
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString"></param>
        public OrderRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một đơn hàng mới
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> AddAsync(Order data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                            VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Bổ sung một mặt hàng vào đơn hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                            VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Xóa đơn hàng dựa vào mã
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                                    DELETE FROM Orders WHERE OrderID = @OrderID;";
                        var result = await connection.ExecuteAsync(sql, new { OrderID = orderID }, transaction) > 0;
                        await transaction.CommitAsync();
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID"></param>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
                var result = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Lấy thông tin đơn hàng dựa trên mã
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT o.*, e.FullName as EmployeeName, 
                                   c.CustomerName, c.ContactName as CustomerContactName, c.Email as CustomerEmail, c.Phone as CustomerPhone, c.Address as CustomerAddress,
                                   s.ShipperName, s.Phone as ShipperPhone,
                                   ISNULL((SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID), 0) as DetailsTotalValue
                            FROM Orders o
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE o.OrderID = @OrderID";
                var data = await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID"></param>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                            FROM OrderDetails od
                            JOIN Products p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID";
                var data = await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng có phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                SearchValue = input.SearchValue
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT o.*, e.FullName as EmployeeName, 
                                   c.CustomerName, c.ContactName as CustomerContactName, c.Email as CustomerEmail, c.Phone as CustomerPhone, c.Address as CustomerAddress,
                                   s.ShipperName, s.Phone as ShipperPhone
                            FROM Orders o
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE (@Status = 0 OR o.Status = @Status)
                                AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                                AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                                AND (c.CustomerName LIKE @SearchValue OR c.ContactName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue OR @SearchValue = N'')
                            ORDER BY o.OrderTime DESC
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE (@Status = 0 OR o.Status = @Status)
                                AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                                AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                                AND (c.CustomerName LIKE @SearchValue OR c.ContactName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue OR @SearchValue = N'');";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize,
                    Status = (int)input.Status,
                    DateFrom = input.DateFrom,
                    DateTo = input.DateTo
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<OrderViewInfo>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách thành phần đơn hàng
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                            FROM OrderDetails od
                            JOIN Products p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID";
                var data = (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Orders 
                            SET CustomerID = @CustomerID, 
                                OrderTime = @OrderTime, 
                                DeliveryProvince = @DeliveryProvince, 
                                DeliveryAddress = @DeliveryAddress, 
                                EmployeeID = @EmployeeID, 
                                AcceptTime = @AcceptTime, 
                                ShipperID = @ShipperID, 
                                ShippedTime = @ShippedTime, 
                                FinishedTime = @FinishedTime, 
                                Status = @Status
                            WHERE OrderID = @OrderID";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Cập nhật thông tin một thành phần của đơn hàng (số lượng và giá bán)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE OrderDetails 
                            SET Quantity = @Quantity, 
                                SalePrice = @SalePrice
                            WHERE OrderID = @OrderID AND ProductID = @ProductID";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }
        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        /// <param name="orderID"></param>
        /// <param name="status"></param>
        /// <param name="acceptTime"></param>
        /// <param name="finishedTime"></param>
        /// <param name="employeeID"></param>
        /// <returns></returns>
        public async Task<bool> UpdateStatusAsync(int orderID, OrderStatusEnum status, DateTime? acceptTime, DateTime? finishedTime, int? employeeID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Orders 
                            SET Status = @Status, 
                                AcceptTime = COALESCE(@AcceptTime, AcceptTime), 
                                FinishedTime = COALESCE(@FinishedTime, FinishedTime),
                                EmployeeID = COALESCE(@EmployeeID, EmployeeID)
                            WHERE OrderID = @OrderID";
                var result = await connection.ExecuteAsync(sql, new { OrderID = orderID, Status = (int)status, AcceptTime = acceptTime, FinishedTime = finishedTime, EmployeeID = employeeID }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }
    }
}
