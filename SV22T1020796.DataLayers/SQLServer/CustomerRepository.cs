using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Partner;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customer) trên SQL Server
    /// </summary>
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public CustomerRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một khách hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng cần bổ sung</param>
        /// <returns>Mã của khách hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                            VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<int>(sql, new {
                    data.CustomerName, data.ContactName, data.Province, data.Address, data.Phone, data.Email,
                    Password = CryptHelper.HashMD5(data.Password), data.IsLocked
                });
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Xóa một khách hàng dựa vào mã id
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Customers WHERE CustomerID = @CustomerID";
                var result = await connection.ExecuteAsync(sql, new { CustomerID = id }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Lấy thông tin của một khách hàng dựa vào mã id
        /// </summary>
        /// <param name="id">Mã khách hàng cần lấy</param>
        /// <returns>Thông tin khách hàng hoặc null nếu không tồn tại</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Customers WHERE CustomerID = @CustomerID";
                var data = await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có dữ liệu liên quan (ví dụ: trong bảng đơn hàng) hay không
        /// </summary>
        /// <param name="id">Mã khách hàng cần kiểm tra</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // Kiểm tra sự tồn tại của khách hàng trong bảng Orders (Đơn hàng)
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerID = @CustomerID) THEN 1 
                                ELSE 0 
                            END";
                var result = await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách khách hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Tham số tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang chứa danh sách khách hàng</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                SearchValue = input.SearchValue
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT * FROM Customers 
                            WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                            ORDER BY CustomerName
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Customers 
                            WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<Customer>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }



        /// <summary>
        /// Cập nhật thông tin của một khách hàng
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Customers 
                            SET CustomerName = @CustomerName, 
                                ContactName = @ContactName, 
                                Province = @Province, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email,
                                Password = @Password,
                                IsLocked = @IsLocked
                            WHERE CustomerID = @CustomerID";
                var result = await connection.ExecuteAsync(sql, new {
                    data.CustomerName, data.ContactName, data.Province, data.Address, data.Phone, data.Email,
                    Password = (data.Password?.Length == 32) ? data.Password : CryptHelper.HashMD5(data.Password),
                    data.IsLocked, data.CustomerID
                }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ (không bị trùng) hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">Mã khách hàng (0 nếu là khách hàng mới)</param>
        /// <returns>True nếu email hợp lệ</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT COUNT(*) FROM Customers 
                            WHERE Email = @Email AND CustomerID <> @CustomerID";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, CustomerID = id });
                await connection.CloseAsync();
                return count == 0;
            }
        }
    }
}