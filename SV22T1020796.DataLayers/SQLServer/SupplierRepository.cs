using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Partner;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp (Supplier) trên SQL Server
    /// </summary>
    public class SupplierRepository : BaseRepository, ISupplierRepository
    {
        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public SupplierRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần bổ sung</param>
        /// <returns>Mã của nhà cung cấp vừa được bổ sung</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            int id = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                            VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                            SELECT @@IDENTITY;";
                id = await connection.ExecuteScalarAsync<int>(sql, data);
                await connection.CloseAsync();
            }
            return id;
        }

        /// <summary>
        /// Xóa một nhà cung cấp dựa vào mã id
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                result = await connection.ExecuteAsync(sql, new { SupplierID = id }) > 0;
                await connection.CloseAsync();
            }
            return result;
        }

        /// <summary>
        /// Lấy thông tin của một nhà cung cấp dựa vào mã id
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần lấy</param>
        /// <returns>Thông tin nhà cung cấp hoặc null nếu không tồn tại</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            Supplier? data = null;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
                data = await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
                await connection.CloseAsync();
            }
            return data;
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có dữ liệu liên quan (ví dụ: trong bảng mặt hàng) hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần kiểm tra</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // Giả sử kiểm tra trong bảng Products (Mặt hàng)
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM Products WHERE SupplierID = @SupplierID) THEN 1 
                                ELSE 0 
                            END";
                result = await connection.ExecuteScalarAsync<bool>(sql, new { SupplierID = id });
                await connection.CloseAsync();
            }
            return result;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhà cung cấp dưới dạng phân trang
        /// </summary>
        /// <param name="input">Tham số tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang chứa danh sách nhà cung cấp</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT * FROM Suppliers 
                            WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                            ORDER BY SupplierName
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Suppliers 
                            WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<Supplier>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Suppliers 
                            SET SupplierName = @SupplierName, 
                                ContactName = @ContactName, 
                                Province = @Province, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email
                            WHERE SupplierID = @SupplierID";
                result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
            }
            return result;
        }

        /// <summary>
        /// Kiểm tra xem email của nhà cung cấp có bị trùng hay không.
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="supplierID">Mã nhà cung cấp (bằng 0 nếu là thêm mới, khác 0 nếu là cập nhật).</param>
        /// <returns>True nếu email hợp lệ (không bị trùng), ngược lại False.</returns>
        public async Task<bool> ValidateEmailAsync(string email, int supplierID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM Suppliers WHERE Email = @Email AND SupplierID <> @SupplierID) THEN 1 
                                ELSE 0 
                            END";
                bool isDuplicated = await connection.ExecuteScalarAsync<bool>(sql, new { Email = email, SupplierID = supplierID });
                await connection.CloseAsync();
                return !isDuplicated;
            }
        }
    }

    /// <summary>
    /// Lớp cơ sở cho các Repository sử dụng SQL Server
    /// </summary>
    public abstract class BaseRepository
    {
        protected readonly string connectionString;

        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        protected BaseRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }
    }
}